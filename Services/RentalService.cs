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

        public RentalService(
            IRepository<int, Rental> rentalRepository,
            IRepository<int, Movie> movieRepository,
            IRepository<int, User> userRepository,
            IRepository<int, Payment> paymentRepository)
        {
            _rentalRepository = rentalRepository;
            _movieRepository = movieRepository;
            _userRepository = userRepository;
            _paymentRepository = paymentRepository;
        }

        public async Task<RentalResponseDto> RentMovie(RentalCreateDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null)
                throw new EntityNotFoundException("User", dto.UserId);

            var movie = await _movieRepository.GetByIdAsync(dto.MovieId);
            if (movie == null)
                throw new EntityNotFoundException("Movie", dto.MovieId);

            if (!movie.IsActive)
                throw new BusinessRuleViolationException(
                    $"'{movie.Title}' is not available for rent.");

            // Check active rental using StoredStatus + ExpiryDate
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

            await _paymentRepository.AddAsync(new Payment
            {
                UserId = dto.UserId,
                RentalId = rental.Id,
                MovieId = dto.MovieId,
                Amount = movie.RentalPrice * dto.DurationDays,
                Method = "Online",
                Status = "Completed",
                PaymentDate = DateTime.UtcNow
            });

            return MapToDto(rental, movie, user);
        }

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

            // Write to StoredStatus — not the computed Status
            rental.StoredStatus = "Returned";
            await _rentalRepository.UpdateAsync(rentalId, rental);

            return MapToDto(rental, rental.Movie!, rental.User!);
        }

        public async Task<RentalResponseDto> GetRental(int id)
        {
            var rentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);
            var rental = rentals.FirstOrDefault(r => r.Id == id);
            if (rental == null)
                throw new EntityNotFoundException("Rental", id);

            return MapToDto(rental, rental.Movie!, rental.User!);
        }

        public async Task<IEnumerable<RentalResponseDto>> GetRentalsByUser(
            int userId)
        {
            var rentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);

            return rentals
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RentalDate)
                .Select(r => MapToDto(r, r.Movie!, r.User!));
        }

        public async Task<IEnumerable<RentalResponseDto>> GetActiveRentals()
        {
            var rentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);

            // Status is computed — filters correctly
            return rentals
                .Where(r => r.Status == "Active")
                .Select(r => MapToDto(r, r.Movie!, r.User!));
        }

        // Status computed automatically from ExpiryDate — no background job needed
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
                Status = rental.Status    // computed property
            };
    }
}