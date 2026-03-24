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
        private readonly AuditLogService _auditLog;

        public RentalService(
            IRepository<int, Rental> rentalRepository,
            IRepository<int, Movie> movieRepository,
            IRepository<int, User> userRepository,
            IRepository<int, Payment> paymentRepository,
            AuditLogService auditLog)
        {
            _rentalRepository = rentalRepository;
            _movieRepository = movieRepository;
            _userRepository = userRepository;
            _paymentRepository = paymentRepository;
            _auditLog = auditLog;
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

            // ✅ Check paused/inactive — logs the attempt too
            if (!movie.IsActive)
            {
                await _auditLog.LogAsync(
                    user.UserId, user.UserName, user.Role.ToString(),
                    $"Attempted to rent '{movie.Title}' — movie is temporarily unavailable.",
                    "MOVIE_PAUSED");

                throw new BusinessRuleViolationException(
                    $"'{movie.Title}' is temporarily unavailable for rental.");
            }

            // Check existing active rental
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
            await _paymentRepository.AddAsync(new Payment
            {
                UserId = dto.UserId,
                RentalId = rental.Id,
                MovieId = dto.MovieId,
                Amount = amount,
                Method = "Online",
                Status = "Completed",
                PaymentDate = DateTime.UtcNow
            });

            // ✅ Audit: successful rental + payment
            await _auditLog.LogAsync(
                user.UserId, user.UserName, user.Role.ToString(),
                $"Rented '{movie.Title}' for {dto.DurationDays} day(s). " +
                $"Amount paid: ₹{amount}. Expires: {rental.ExpiryDate:dd MMM yyyy}.",
                "");

            return MapToDto(rental, movie, user);
        }

        // ── RETURN MOVIE ──────────────────────────────────────────
        public async Task<RentalResponseDto> ReturnMovie(int rentalId)
        {
            var rentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);
            var rental = rentals.FirstOrDefault(r => r.Id == rentalId);
            if (rental == null)
                throw new EntityNotFoundException("Rental", rentalId);

            if (rental.Status == "Returned")
                throw new BusinessRuleViolationException(
                    "This rental has already been returned.");

            rental.StoredStatus = "Returned";
            await _rentalRepository.UpdateAsync(rentalId, rental);

            // ✅ Audit: return
            await _auditLog.LogAsync(
                rental.UserId,
                rental.User!.UserName,
                rental.User.Role.ToString(),
                $"Returned '{rental.Movie!.Title}'. Rental ID: {rentalId}.",
                "");

            return MapToDto(rental, rental.Movie!, rental.User!);
        }

        // ── GET RENTAL ────────────────────────────────────────────
        public async Task<RentalResponseDto> GetRental(int id)
        {
            var rentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);
            var rental = rentals.FirstOrDefault(r => r.Id == id);
            if (rental == null)
                throw new EntityNotFoundException("Rental", id);
            return MapToDto(rental, rental.Movie!, rental.User!);
        }

        // ── GET BY USER ───────────────────────────────────────────
        public async Task<IEnumerable<RentalResponseDto>> GetRentalsByUser(int userId)
        {
            var rentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);
            return rentals
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RentalDate)
                .Select(r => MapToDto(r, r.Movie!, r.User!));
        }

        // ── GET ACTIVE ────────────────────────────────────────────
        public async Task<IEnumerable<RentalResponseDto>> GetActiveRentals()
        {
            var rentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);
            return rentals
                .Where(r => r.Status == "Active")
                .Select(r => MapToDto(r, r.Movie!, r.User!));
        }

        // ── MAPPER ────────────────────────────────────────────────
        private static RentalResponseDto MapToDto(
            Rental rental, Movie movie, User user) => new()
            {
                Id = rental.Id,
                UserId = rental.UserId,
                UserName = user.UserName,
                MovieId = rental.MovieId,
                MovieTitle = movie.Title,
                RentalDate = rental.RentalDate,
                ExpiryDate = rental.ExpiryDate,
                Status = rental.Status
            };
    }
}