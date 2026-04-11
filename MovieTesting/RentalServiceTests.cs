using Microsoft.Extensions.Logging;
using Moq;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using MovieRentalApp.Services;

namespace MovieTesting
{
    public class RentalServiceTests
    {
        private readonly Mock<IRepository<int, Rental>> _rentalRepo = new();
        private readonly Mock<IRepository<int, Movie>> _movieRepo = new();
        private readonly Mock<IRepository<int, User>> _userRepo = new();
        private readonly Mock<IRepository<int, Payment>> _paymentRepo = new();
        private readonly Mock<IWishlistService> _wishlistService = new();
        private readonly AuditLogService _auditLog = new FakeAuditLogService();
        private readonly Mock<ILogger<RentalService>> _logger = new();
        private readonly RentalService _sut;

        public RentalServiceTests()
        {
            _sut = new RentalService(
                _rentalRepo.Object, _movieRepo.Object, _userRepo.Object,
                _paymentRepo.Object, _wishlistService.Object,
                _auditLog, _logger.Object);
        }

        private static User MakeUser(int id = 1) =>
            new() { UserId = id, UserName = "Alice", UserEmail = "a@a.com", Role = UserRole.Customer, IsActive = true };

        private static Movie MakeMovie(int id = 1, bool active = true) =>
            new() { Id = id, Title = "Inception", RentalPrice = 50, IsActive = active };

        // ── RentMovie ─────────────────────────────────────────────

        [Fact]
        public async Task RentMovie_Valid_ReturnsRentalResponseDto()
        {
            var dto = new RentalCreateDto { UserId = 1, MovieId = 1, DurationDays = 7, PaymentMethod = "UPI" };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _rentalRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                       .ReturnsAsync(Enumerable.Empty<Rental>());
            _rentalRepo.Setup(r => r.AddAsync(It.IsAny<Rental>()))
                       .ReturnsAsync(new Rental { Id = 1, UserId = 1, MovieId = 1, RentalDate = DateTime.UtcNow, ExpiryDate = DateTime.UtcNow.AddDays(7), StoredStatus = "Active" });
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync(new Payment());
            _wishlistService.Setup(w => w.RemoveByUserAndMovieAsync(1, 1)).Returns(Task.CompletedTask);

            var result = await _sut.RentMovie(dto);

            Assert.Equal("Inception", result.MovieTitle);
            Assert.Equal("Active", result.Status);
        }

        [Fact]
        public async Task RentMovie_UserNotFound_ThrowsEntityNotFoundException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.RentMovie(new RentalCreateDto { UserId = 99, MovieId = 1, DurationDays = 7 }));
        }

        [Fact]
        public async Task RentMovie_MovieNotFound_ThrowsEntityNotFoundException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Movie?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.RentMovie(new RentalCreateDto { UserId = 1, MovieId = 99, DurationDays = 7 }));
        }

        [Fact]
        public async Task RentMovie_InactiveMovie_ThrowsBusinessRuleViolation()
        {
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie(active: false));

            await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
                _sut.RentMovie(new RentalCreateDto { UserId = 1, MovieId = 1, DurationDays = 7 }));
        }

        [Fact]
        public async Task RentMovie_AlreadyRented_ThrowsBusinessRuleViolation()
        {
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _rentalRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                       .ReturnsAsync(new[] { new Rental { StoredStatus = "Active", ExpiryDate = DateTime.UtcNow.AddDays(5) } });

            await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
                _sut.RentMovie(new RentalCreateDto { UserId = 1, MovieId = 1, DurationDays = 7 }));
        }

        // ── ReturnMovie ───────────────────────────────────────────

        [Fact]
        public async Task ReturnMovie_NotFound_ThrowsEntityNotFoundException()
        {
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                       .ReturnsAsync(Enumerable.Empty<Rental>());

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.ReturnMovie(99));
        }

        [Fact]
        public async Task ReturnMovie_AlreadyReturned_ThrowsBusinessRuleViolation()
        {
            var rental = new Rental { Id = 1, UserId = 1, MovieId = 1, StoredStatus = "Returned", RentalDate = DateTime.UtcNow.AddDays(-2), ExpiryDate = DateTime.UtcNow.AddDays(5), Movie = MakeMovie(), User = MakeUser() };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                       .ReturnsAsync(new[] { rental });

            await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _sut.ReturnMovie(1));
        }

        [Fact]
        public async Task ReturnMovie_Valid_SetsStatusReturned()
        {
            var rental = new Rental { Id = 1, UserId = 1, MovieId = 1, StoredStatus = "Active", RentalDate = DateTime.UtcNow.AddDays(-2), ExpiryDate = DateTime.UtcNow.AddDays(5), Movie = MakeMovie(), User = MakeUser() };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                       .ReturnsAsync(new[] { rental });
            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(Enumerable.Empty<Payment>());
            _rentalRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Rental>())).ReturnsAsync(rental);

            var result = await _sut.ReturnMovie(1);

            Assert.Equal("Returned", result.Status);
        }

        // ── GetRentalsByUser ──────────────────────────────────────

        [Fact]
        public async Task GetRentalsByUser_ReturnsUserRentals()
        {
            var rental = new Rental { Id = 1, UserId = 1, MovieId = 1, StoredStatus = "Active", RentalDate = DateTime.UtcNow, ExpiryDate = DateTime.UtcNow.AddDays(7), Movie = MakeMovie(), User = MakeUser() };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                       .ReturnsAsync(new[] { rental });
            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(Enumerable.Empty<Payment>());

            var result = await _sut.GetRentalsByUser(1);

            Assert.Single(result);
        }

        // ── IsEligibleToRateAsync ─────────────────────────────────

        [Fact]
        public async Task IsEligibleToRate_ReturnedRental_ReturnsTrue()
        {
            _rentalRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                       .ReturnsAsync(new[] { new Rental { StoredStatus = "Returned", ExpiryDate = DateTime.UtcNow.AddDays(5) } });

            var result = await _sut.IsEligibleToRateAsync(1, 1);

            Assert.True(result);
        }

        [Fact]
        public async Task IsEligibleToRate_ActiveNotExpired_ReturnsFalse()
        {
            _rentalRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                       .ReturnsAsync(new[] { new Rental { StoredStatus = "Active", ExpiryDate = DateTime.UtcNow.AddDays(5) } });

            var result = await _sut.IsEligibleToRateAsync(1, 1);

            Assert.False(result);
        }

        [Fact]
        public async Task IsEligibleToRate_NoRentals_ReturnsFalse()
        {
            _rentalRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                       .ReturnsAsync(Enumerable.Empty<Rental>());

            var result = await _sut.IsEligibleToRateAsync(1, 1);

            Assert.False(result);
        }

        // ── ReturnMovie with refund ───────────────────────────────

        [Fact]
        public async Task ReturnMovie_WithinRefundWindow_IssuesRefund()
        {
            var user = MakeUser();
            var movie = MakeMovie();
            // Rented less than 1 day ago → within refund window
            var rental = new Rental
            {
                Id = 1, UserId = 1, MovieId = 1,
                StoredStatus = "Active",
                RentalDate = DateTime.UtcNow.AddHours(-2),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                Movie = movie, User = user
            };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                       .ReturnsAsync(new[] { rental });
            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(new[] { new Payment { Id = 1, RentalId = 1, Amount = 350, Status = "Completed" } });
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync(new Payment());
            _rentalRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Rental>())).ReturnsAsync(rental);

            var result = await _sut.ReturnMovie(1);

            Assert.Equal("Returned", result.Status);
            Assert.True(result.RefundAmount > 0);
        }

        // ── GetRentalsByUser with payment lookup ──────────────────

        [Fact]
        public async Task GetRentalsByUser_WithPayment_PopulatesTotalPaid()
        {
            var user = MakeUser();
            var movie = MakeMovie();
            var rental = new Rental { Id = 1, UserId = 1, MovieId = 1, StoredStatus = "Active", RentalDate = DateTime.UtcNow, ExpiryDate = DateTime.UtcNow.AddDays(7), Movie = movie, User = user };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                       .ReturnsAsync(new[] { rental });
            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(new[] { new Payment { RentalId = 1, Amount = 350, Status = "Completed" } });

            var result = await _sut.GetRentalsByUser(1);

            Assert.Equal(350, result.First().TotalPaid);
        }
    }
}
