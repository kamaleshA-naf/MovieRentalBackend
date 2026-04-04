using Microsoft.Extensions.Logging;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Services
{
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

            if (rental.StoredStatus == "Returned")
                throw new BusinessRuleViolationException(
                    "This rental has already been returned.");

            // ── Refund calculation ────────────────────────────────
            var daysSinceRent = (DateTime.UtcNow - rental.RentalDate).TotalDays;
            var canReturn = daysSinceRent <= RefundWindowDays;

            decimal refundAmount = 0m;
            if (canReturn)
            {
                // Find the original payment for this rental
                var payments = await _paymentRepository.FindAsync(
                    p => p.RentalId == rentalId && p.Status == "Completed");
                var original = payments.FirstOrDefault();

                if (original != null)
                {
                    refundAmount = Math.Round(original.Amount * RefundRate, 2);

                    // Save refund as a separate payment record
                    await _paymentRepository.AddAsync(new Payment
                    {
                        UserId = rental.UserId,
                        RentalId = rentalId,
                        MovieId = rental.MovieId,
                        Amount = -refundAmount,          // negative = refund
                        Method = "Refund",
                        Status = "Refunded",
                        PaymentDate = DateTime.UtcNow
                    });

                    _logger.LogInformation(
                        "Refund of ₹{Refund} issued for rental {RentalId} (within {Days:F2} days)",
                        refundAmount, rentalId, daysSinceRent);
                }
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
        public async Task<IEnumerable<RentalResponseDto>> GetRentalsByUser(int userId)
        {
            var rentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);

            var userRentals = rentals
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RentalDate)
                .ToList();

            var result = new List<RentalResponseDto>();
            foreach (var r in userRentals)
            {
                var paid = await GetTotalPaid(r.Id);
                result.Add(MapToDto(r, r.Movie!, r.User!, totalPaid: paid));
            }
            return result;
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
            var canReturn = rental.StoredStatus == "Active" &&
                            (DateTime.UtcNow - rental.RentalDate).TotalDays <= RefundWindowDays;

            return new RentalResponseDto
            {
                Id = rental.Id,
                UserId = rental.UserId,
                UserName = user.UserName,
                MovieId = rental.MovieId,
                MovieTitle = movie?.Title ?? "Movie Unavailable",
                RentalDate = rental.RentalDate,
                ExpiryDate = rental.ExpiryDate,
                ReturnDate = rental.ReturnDate,
                Status = rental.Status,
                CanReturn = canReturn,
                RefundAmount = refundAmount,
                RentalPrice = movie?.RentalPrice ?? 0,
                TotalPaid = totalPaid,
                MovieIsActive = movie?.IsActive ?? false
            };
        }
    }
}
