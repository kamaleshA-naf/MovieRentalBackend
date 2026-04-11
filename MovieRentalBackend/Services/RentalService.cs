using Microsoft.Extensions.Logging;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using System.Diagnostics;

namespace MovieRentalApp.Services
{
    [DebuggerNonUserCode]
    public class RentalService : IRentalService
    {
        private readonly IRepository<int, Rental> _rentalRepository;
        private readonly IRepository<int, Movie> _movieRepository;
        private readonly IRepository<int, User> _userRepository;
        private readonly IRepository<int, Payment> _paymentRepository;
        private readonly IWishlistService _wishlistService;
        private readonly AuditLogService _auditLog;
        private readonly ILogger<RentalService> _logger;

        // Refund window: rentals within 1 day get 90% back
        private const double RefundWindowDays = 1.0;
        private const decimal RefundRate = 0.90m;

        public RentalService(
            IRepository<int, Rental> rentalRepository,
            IRepository<int, Movie> movieRepository,
            IRepository<int, User> userRepository,
            IRepository<int, Payment> paymentRepository,
            IWishlistService wishlistService,
            AuditLogService auditLog,
            ILogger<RentalService> logger)
        {
            _rentalRepository = rentalRepository;
            _movieRepository = movieRepository;
            _userRepository = userRepository;
            _paymentRepository = paymentRepository;
            _wishlistService = wishlistService;
            _auditLog = auditLog;
            _logger = logger;
        }

        // ── RENT MOVIE ────────────────────────────────────────────
        public async Task<RentalResponseDto> RentMovie(RentalCreateDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null)
                throw new EntityNotFoundException("User", dto.UserId);

            var movie = await _movieRepository.GetByIdAsync(dto.MovieId);
            if (movie == null)
                throw new EntityNotFoundException("Movie", dto.MovieId);

            if (!movie.IsActive)
            {
                await _auditLog.LogAsync(
                    user.UserId, user.UserName, user.Role.ToString(),
                    $"Attempted to rent '{movie.Title}' — movie is temporarily unavailable.",
                    "MOVIE_PAUSED");
                throw new BusinessRuleViolationException(
                    $"'{movie.Title}' is temporarily unavailable for rental.");
            }

            var existing = await _rentalRepository.FindAsync(
                r => r.UserId == dto.UserId &&
                     r.MovieId == dto.MovieId &&
                     r.StoredStatus == "Active" &&
                     r.ExpiryDate > DateTime.UtcNow);

            if (existing.Any())
                throw new BusinessRuleViolationException(
                    $"You already have '{movie.Title}' rented.");

            var rental = new Rental
            {
                UserId = dto.UserId,
                MovieId = dto.MovieId,
                RentalDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(dto.DurationDays),
                StoredStatus = "Active"
            };
            await _rentalRepository.AddAsync(rental);

            var amount = movie.RentalPrice * dto.DurationDays;

            // Validate payment method
            var validMethods = new[] { "UPI", "Card", "NetBanking" };
            var method = validMethods.Contains(dto.PaymentMethod) ? dto.PaymentMethod : "UPI";

            await _paymentRepository.AddAsync(new Payment
            {
                UserId = dto.UserId,
                RentalId = rental.Id,
                MovieId = dto.MovieId,
                Amount = amount,
                Method = method,
                Status = "Completed",
                PaymentDate = DateTime.UtcNow
            });

            // Remove from wishlist if present — safe, no exception if missing
            await _wishlistService.RemoveByUserAndMovieAsync(dto.UserId, dto.MovieId);
            _logger.LogInformation(
                "User {UserId} rented movie {MovieId} '{Title}' for {Days} day(s). Amount: {Amount}",
                dto.UserId, dto.MovieId, movie.Title, dto.DurationDays, amount);

            await _auditLog.LogAsync(
                user.UserId, user.UserName, user.Role.ToString(),
                $"Rented '{movie.Title}' for {dto.DurationDays} day(s). " +
                $"Amount paid: ₹{amount}. Method: {method}. Expires: {rental.ExpiryDate:dd MMM yyyy}.", "");

            return MapToDto(rental, movie, user, totalPaid: amount);
        }

        // ── RETURN MOVIE ──────────────────────────────────────────
        public async Task<RentalResponseDto> ReturnMovie(int rentalId)
        {
            var rentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);
            var rental = rentals.FirstOrDefault(r => r.Id == rentalId);

            if (rental == null)
                throw new EntityNotFoundException("Rental", rentalId);

            // Reject if already returned
            if (rental.StoredStatus == "Returned")
                throw new BusinessRuleViolationException(
                    "This rental has already been returned.");

            // Reject if expired — cannot return an expired rental
            if (rental.ExpiryDate < DateTime.UtcNow)
                throw new BusinessRuleViolationException(
                    "This rental has expired and cannot be returned.");

            // ── Refund calculation ────────────────────────────────
            var daysSinceRent = (DateTime.UtcNow - rental.RentalDate).TotalDays;
            var withinWindow  = daysSinceRent <= RefundWindowDays;

            decimal refundAmount = 0m;

            // Find the original completed payment to get amount + method
            var payments = await _paymentRepository.FindAsync(
                p => p.RentalId == rentalId && p.Status == "Completed");
            var original = payments.FirstOrDefault();

            if (original != null)
            {
                refundAmount = withinWindow
                    ? Math.Round(original.Amount * RefundRate, 2)
                    : 0m;

                // Always create a Refunded transaction record so it appears in history
                try
                {
                    await _paymentRepository.AddAsync(new Payment
                    {
                        UserId      = rental.UserId,
                        RentalId    = rentalId,
                        MovieId     = rental.MovieId,
                        Amount      = -refundAmount,   // 0 or negative refund value
                        Method      = original.Method, // same method as original payment
                        Status      = "Refunded",
                        PaymentDate = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to create Refunded payment record for rental {RentalId}", rentalId);
                    refundAmount = 0m;
                }

                _logger.LogInformation(
                    "Return for rental {RentalId}: withinWindow={Within}, refund=₹{Refund}",
                    rentalId, withinWindow, refundAmount);
            }

            rental.StoredStatus = "Returned";
            rental.ReturnDate = DateTime.UtcNow;
            await _rentalRepository.UpdateAsync(rentalId, rental);

            await _auditLog.LogAsync(
                rental.UserId,
                rental.User!.UserName,
                rental.User.Role.ToString(),
                $"Returned '{rental.Movie!.Title}'. Rental ID: {rentalId}. " +
                $"Refund: ₹{refundAmount}.", "");

            return MapToDto(rental, rental.Movie!, rental.User!, refundAmount);
        }

        
        // ── GET BY USER ───────────────────────────────────────────
        // status = null → all | "Active" | "Expired" | "Returned"
        public async Task<IEnumerable<RentalResponseDto>> GetRentalsByUser(
            int userId, string? status = null)
        {
            var rentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);

            // Latest rental per movie only — no duplicate cards in UI
            var userRentals = rentals
                .Where(r => r.UserId == userId)
                .GroupBy(r => r.MovieId)
                .Select(g => g.OrderByDescending(r => r.RentalDate).First())
                .OrderByDescending(r => r.RentalDate)
                .ToList();

            // Filter by status if provided (case-insensitive)
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalised = status.Trim().ToLower();
                userRentals = userRentals
                    .Where(r => r.Status.ToLower() == normalised)
                    .ToList();
            }

            var result = new List<RentalResponseDto>();
            foreach (var r in userRentals)
            {
                var paid = await GetTotalPaid(r.Id);
                result.Add(MapToDto(r, r.Movie!, r.User!, totalPaid: paid));
            }
            return result;
        }

        // ── SYNC EXPIRED ──────────────────────────────────────────
        // Writes "Expired" into StoredStatus for all past-expiry Active rentals
        public async Task SyncExpiredRentalsAsync()
        {
            var all = await _rentalRepository.FindAsync(
                r => r.StoredStatus == "Active" && r.ExpiryDate < DateTime.UtcNow);

            foreach (var r in all)
            {
                r.StoredStatus = "Expired";
                await _rentalRepository.UpdateAsync(r.Id, r);
            }
        }

        // ── BACKFILL REFUNDED PAYMENTS ────────────────────────────
        // Creates Refunded payment records for returned rentals that have none.
        // Run once to fix historical data.
        public async Task<int> BackfillRefundedPaymentsAsync()
        {
            var returnedRentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);

            var returned = returnedRentals
                .Where(r => r.StoredStatus == "Returned")
                .ToList();

            var allPayments = await _paymentRepository.GetAllAsync();
            var paymentList = allPayments.ToList();

            int created = 0;
            foreach (var rental in returned)
            {
                // Skip if a Refunded record already exists for this rental
                var alreadyHasRefund = paymentList.Any(
                    p => p.RentalId == rental.Id && p.Status == "Refunded");
                if (alreadyHasRefund) continue;

                // Find the original Completed payment to get method
                var original = paymentList.FirstOrDefault(
                    p => p.RentalId == rental.Id && p.Status == "Completed");

                await _paymentRepository.AddAsync(new Payment
                {
                    UserId      = rental.UserId,
                    RentalId    = rental.Id,
                    MovieId     = rental.MovieId,
                    Amount      = 0m,                          // no refund (outside window)
                    Method      = original?.Method ?? "UPI",   // preserve original method
                    Status      = "Refunded",
                    PaymentDate = rental.ReturnDate ?? rental.ExpiryDate
                });
                created++;
            }

            _logger.LogInformation("BackfillRefundedPayments: created {Count} records", created);
            return created;
        }

        // Lookup the original completed payment amount for a rental
        private async Task<decimal> GetTotalPaid(int rentalId)
        {
            var payments = await _paymentRepository.FindAsync(
                p => p.RentalId == rentalId && p.Status == "Completed");
            return payments.FirstOrDefault()?.Amount ?? 0m;
        }

        // ── RATING ELIGIBILITY ────────────────────────────────────
        public async Task<bool> IsEligibleToRateAsync(int userId, int movieId)
        {
            var rentals = await _rentalRepository.FindAsync(
                r => r.UserId == userId && r.MovieId == movieId);

            return rentals.Any(r =>
                r.StoredStatus == "Returned" ||
                (r.StoredStatus == "Active" && r.ExpiryDate < DateTime.UtcNow));
        }

        // ── MAPPER ────────────────────────────────────────────────
        private static RentalResponseDto MapToDto(
            Rental rental, Movie movie, User user,
            decimal refundAmount = 0m, decimal totalPaid = 0m)
        {
            var status = rental.Status; // computed: Active | Returned | Expired
            var canReturn = status == "Active" &&
                            (DateTime.UtcNow - rental.RentalDate).TotalDays <= RefundWindowDays;

            return new RentalResponseDto
            {
                Id            = rental.Id,
                UserId        = rental.UserId,
                UserName      = user.UserName,
                MovieId       = rental.MovieId,
                MovieTitle    = movie?.Title ?? "Movie Unavailable",
                RentalDate    = rental.RentalDate,
                ExpiryDate    = rental.ExpiryDate,
                ReturnDate    = rental.ReturnDate,
                Status        = status,
                IsActive      = status == "Active",
                CanReturn     = canReturn,
                RefundAmount  = refundAmount,
                RentalPrice   = movie?.RentalPrice ?? 0,
                TotalPaid     = totalPaid,
                MovieIsActive = movie?.IsActive ?? false
            };
        }
    }
}
