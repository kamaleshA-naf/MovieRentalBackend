using Microsoft.Extensions.Logging;
using Moq;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using MovieRentalApp.Services;

namespace MovieTesting
{
    /// <summary>
    /// Additional coverage for RentalService paths not covered in RentalServiceTests.
    /// </summary>
    public class RentalServiceAdditionalTests
    {
        private readonly Mock<IRepository<int, Rental>>  _rentalRepo  = new();
        private readonly Mock<IRepository<int, Movie>>   _movieRepo   = new();
        private readonly Mock<IRepository<int, User>>    _userRepo    = new();
        private readonly Mock<IRepository<int, Payment>> _paymentRepo = new();
        private readonly Mock<IWishlistService>          _wishlist    = new();
        private readonly AuditLogService                 _auditLog    = new FakeAuditLogService();
        private readonly Mock<ILogger<RentalService>>    _logger      = new();
        private readonly RentalService _sut;

        public RentalServiceAdditionalTests()
        {
            _sut = new RentalService(
                _rentalRepo.Object, _movieRepo.Object, _userRepo.Object,
                _paymentRepo.Object, _wishlist.Object, _auditLog, _logger.Object);
        }

        private static User  MakeUser(int id = 1)  => new() { UserId = id, UserName = "Alice", UserEmail = "a@a.com", Role = UserRole.Customer, IsActive = true };
        private static Movie MakeMovie(int id = 1) => new() { Id = id, Title = "Inception", RentalPrice = 50, IsActive = true };

        // ── ReturnMovie: expired rental cannot be returned ────────

        [Fact]
        public async Task ReturnMovie_ExpiredRental_ThrowsBusinessRuleViolation()
        {
            var rental = new Rental
            {
                Id = 1, UserId = 1, MovieId = 1,
                StoredStatus = "Active",
                RentalDate   = DateTime.UtcNow.AddDays(-10),
                ExpiryDate   = DateTime.UtcNow.AddDays(-1), // already expired
                Movie = MakeMovie(), User = MakeUser()
            };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                .ReturnsAsync(new[] { rental });

            await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _sut.ReturnMovie(1));
        }

        // ── ReturnMovie: outside refund window → refund = 0 ──────

        [Fact]
        public async Task ReturnMovie_OutsideRefundWindow_RefundIsZero()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var rental = new Rental
            {
                Id = 1, UserId = 1, MovieId = 1,
                StoredStatus = "Active",
                RentalDate   = DateTime.UtcNow.AddDays(-3), // > 1 day ago
                ExpiryDate   = DateTime.UtcNow.AddDays(4),
                Movie = movie, User = user
            };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                .ReturnsAsync(new[] { rental });
            _paymentRepo.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>()))
                .ReturnsAsync(new[] { new Payment { Id = 1, RentalId = 1, Amount = 350, Status = "Completed", Method = "UPI" } });
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync(new Payment());
            _rentalRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Rental>())).ReturnsAsync(rental);

            var result = await _sut.ReturnMovie(1);

            Assert.Equal(0, result.RefundAmount);
        }

        // ── ReturnMovie: no original payment → no refund record ──

        [Fact]
        public async Task ReturnMovie_NoOriginalPayment_ReturnsWithZeroRefund()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var rental = new Rental
            {
                Id = 1, UserId = 1, MovieId = 1,
                StoredStatus = "Active",
                RentalDate   = DateTime.UtcNow.AddHours(-1),
                ExpiryDate   = DateTime.UtcNow.AddDays(7),
                Movie = movie, User = user
            };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                .ReturnsAsync(new[] { rental });
            _paymentRepo.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>()))
                .ReturnsAsync(Enumerable.Empty<Payment>()); // no payment record
            _rentalRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Rental>())).ReturnsAsync(rental);

            var result = await _sut.ReturnMovie(1);

            Assert.Equal(0, result.RefundAmount);
        }

        // ── RentMovie: invalid payment method defaults to UPI ─────

        [Fact]
        public async Task RentMovie_InvalidPaymentMethod_DefaultsToUPI()
        {
            var dto = new RentalCreateDto { UserId = 1, MovieId = 1, DurationDays = 7, PaymentMethod = "Bitcoin" };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _rentalRepo.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                .ReturnsAsync(Enumerable.Empty<Rental>());
            _rentalRepo.Setup(r => r.AddAsync(It.IsAny<Rental>()))
                .ReturnsAsync(new Rental { Id = 1, UserId = 1, MovieId = 1, StoredStatus = "Active", RentalDate = DateTime.UtcNow, ExpiryDate = DateTime.UtcNow.AddDays(7) });

            Payment? capturedPayment = null;
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                .Callback<Payment>(p => capturedPayment = p)
                .ReturnsAsync(new Payment());
            _wishlist.Setup(w => w.RemoveByUserAndMovieAsync(1, 1)).Returns(Task.CompletedTask);

            await _sut.RentMovie(dto);

            Assert.Equal("UPI", capturedPayment?.Method);
        }

        // ── GetRentalsByUser: status filter ───────────────────────

        [Fact]
        public async Task GetRentalsByUser_WithStatusFilter_ReturnsOnlyMatching()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var active  = new Rental { Id = 1, UserId = 1, MovieId = 1, StoredStatus = "Active",   RentalDate = DateTime.UtcNow,             ExpiryDate = DateTime.UtcNow.AddDays(7),  Movie = movie, User = user };
            var expired = new Rental { Id = 2, UserId = 1, MovieId = 2, StoredStatus = "Expired",  RentalDate = DateTime.UtcNow.AddDays(-10), ExpiryDate = DateTime.UtcNow.AddDays(-3), Movie = new Movie { Id = 2, Title = "Avatar" }, User = user };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                .ReturnsAsync(new[] { active, expired });
            _paymentRepo.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>()))
                .ReturnsAsync(Enumerable.Empty<Payment>());

            var result = await _sut.GetRentalsByUser(1, "Active");

            Assert.Single(result);
            Assert.Equal("Inception", result.First().MovieTitle);
        }

        [Fact]
        public async Task GetRentalsByUser_NoStatusFilter_ReturnsAll()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var r1 = new Rental { Id = 1, UserId = 1, MovieId = 1, StoredStatus = "Active",  RentalDate = DateTime.UtcNow,             ExpiryDate = DateTime.UtcNow.AddDays(7),  Movie = movie, User = user };
            var r2 = new Rental { Id = 2, UserId = 1, MovieId = 2, StoredStatus = "Returned", RentalDate = DateTime.UtcNow.AddDays(-5), ExpiryDate = DateTime.UtcNow.AddDays(-1), Movie = new Movie { Id = 2, Title = "Avatar" }, User = user };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                .ReturnsAsync(new[] { r1, r2 });
            _paymentRepo.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>()))
                .ReturnsAsync(Enumerable.Empty<Payment>());

            var result = await _sut.GetRentalsByUser(1);

            Assert.Equal(2, result.Count());
        }

        // ── SyncExpiredRentalsAsync ───────────────────────────────

        [Fact]
        public async Task SyncExpiredRentalsAsync_UpdatesExpiredRentals()
        {
            var expired = new Rental { Id = 1, StoredStatus = "Active", ExpiryDate = DateTime.UtcNow.AddDays(-1) };
            _rentalRepo.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                .ReturnsAsync(new[] { expired });
            _rentalRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Rental>())).ReturnsAsync(expired);

            await _sut.SyncExpiredRentalsAsync();

            _rentalRepo.Verify(r => r.UpdateAsync(1, It.Is<Rental>(x => x.StoredStatus == "Expired")), Times.Once);
        }

        [Fact]
        public async Task SyncExpiredRentalsAsync_NoExpired_DoesNothing()
        {
            _rentalRepo.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                .ReturnsAsync(Enumerable.Empty<Rental>());

            await _sut.SyncExpiredRentalsAsync(); // no exception = pass

            _rentalRepo.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Rental>()), Times.Never);
        }

        // ── BackfillRefundedPaymentsAsync ─────────────────────────

        [Fact]
        public async Task BackfillRefundedPaymentsAsync_CreatesRecordsForMissingRefunds()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var returned = new Rental
            {
                Id = 1, UserId = 1, MovieId = 1,
                StoredStatus = "Returned",
                RentalDate   = DateTime.UtcNow.AddDays(-5),
                ExpiryDate   = DateTime.UtcNow.AddDays(-2),
                ReturnDate   = DateTime.UtcNow.AddDays(-3),
                Movie = movie, User = user
            };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                .ReturnsAsync(new[] { returned });
            // No existing refund payment
            _paymentRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new[]
                {
                    new Payment { Id = 1, RentalId = 1, Status = "Completed", Method = "Card", Amount = 350 }
                });
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync(new Payment());

            var count = await _sut.BackfillRefundedPaymentsAsync();

            Assert.Equal(1, count);
            _paymentRepo.Verify(r => r.AddAsync(It.Is<Payment>(p =>
                p.Status == "Refunded" && p.Method == "Card")), Times.Once);
        }

        [Fact]
        public async Task BackfillRefundedPaymentsAsync_SkipsIfRefundAlreadyExists()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var returned = new Rental
            {
                Id = 1, UserId = 1, MovieId = 1,
                StoredStatus = "Returned",
                RentalDate   = DateTime.UtcNow.AddDays(-5),
                ExpiryDate   = DateTime.UtcNow.AddDays(-2),
                Movie = movie, User = user
            };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                .ReturnsAsync(new[] { returned });
            // Refund already exists
            _paymentRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new[]
                {
                    new Payment { Id = 1, RentalId = 1, Status = "Completed", Method = "UPI", Amount = 350 },
                    new Payment { Id = 2, RentalId = 1, Status = "Refunded",  Method = "UPI", Amount = 0 }
                });

            var count = await _sut.BackfillRefundedPaymentsAsync();

            Assert.Equal(0, count);
            _paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
        }

        [Fact]
        public async Task BackfillRefundedPaymentsAsync_NoOriginalPayment_UsesUPIDefault()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var returned = new Rental
            {
                Id = 1, UserId = 1, MovieId = 1,
                StoredStatus = "Returned",
                RentalDate   = DateTime.UtcNow.AddDays(-5),
                ExpiryDate   = DateTime.UtcNow.AddDays(-2),
                Movie = movie, User = user
            };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                .ReturnsAsync(new[] { returned });
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Payment>());
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync(new Payment());

            await _sut.BackfillRefundedPaymentsAsync();

            _paymentRepo.Verify(r => r.AddAsync(It.Is<Payment>(p => p.Method == "UPI")), Times.Once);
        }

        // ── IsEligibleToRate: active but expired by date ──────────

        [Fact]
        public async Task IsEligibleToRate_ActiveButPastExpiry_ReturnsTrue()
        {
            _rentalRepo.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                .ReturnsAsync(new[] { new Rental { StoredStatus = "Active", ExpiryDate = DateTime.UtcNow.AddDays(-1) } });

            var result = await _sut.IsEligibleToRateAsync(1, 1);

            Assert.True(result);
        }

        // ── GetRentalsByUser: deduplication keeps latest ──────────

        [Fact]
        public async Task GetRentalsByUser_DuplicateMovies_KeepsLatestRental()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var older = new Rental { Id = 1, UserId = 1, MovieId = 1, StoredStatus = "Returned", RentalDate = DateTime.UtcNow.AddDays(-10), ExpiryDate = DateTime.UtcNow.AddDays(-3), Movie = movie, User = user };
            var newer = new Rental { Id = 2, UserId = 1, MovieId = 1, StoredStatus = "Active",   RentalDate = DateTime.UtcNow,             ExpiryDate = DateTime.UtcNow.AddDays(7),  Movie = movie, User = user };
            _rentalRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Rental, object>>[]>()))
                .ReturnsAsync(new[] { older, newer });
            _paymentRepo.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>()))
                .ReturnsAsync(Enumerable.Empty<Payment>());

            var result = await _sut.GetRentalsByUser(1);

            Assert.Single(result);
            Assert.Equal("Active", result.First().Status);
        }
    }
}
