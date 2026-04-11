using Moq;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using MovieRentalApp.Services;

namespace MovieTesting
{
    public class PaymentServiceTests
    {
        private readonly Mock<IRepository<int, Payment>> _paymentRepo = new();
        private readonly Mock<IRepository<int, User>>    _userRepo    = new();
        private readonly PaymentService _sut;

        private static readonly GetPaymentsByUserRequestDto DefaultReq =
            new() { PageNumber = 1, PageSize = 50, SortOrder = "desc" };

        public PaymentServiceTests()
        {
            _sut = new PaymentService(_paymentRepo.Object, _userRepo.Object);
        }

        private static User  MakeUser(int id = 1)  =>
            new() { UserId = id, UserName = "Alice", UserEmail = "a@a.com", Role = UserRole.Customer };

        private static Movie MakeMovie(int id = 1) =>
            new() { Id = id, Title = "Inception" };

        // helper — unwrap the paged wrapper
        private static List<PaymentResponseDto> Items(PagedResultDto<PaymentResponseDto> paged)
            => (paged.Data ?? paged.Items ?? Enumerable.Empty<PaymentResponseDto>()).ToList();

        // ── UserNotFound ──────────────────────────────────────────

        [Fact]
        public async Task GetPaymentsByUser_UserNotFound_ThrowsEntityNotFoundException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.GetPaymentsByUser(99, DefaultReq));
        }

        // ── Returns only user's payments ──────────────────────────

        [Fact]
        public async Task GetPaymentsByUser_ReturnsOnlyUserPayments()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _paymentRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, object>>[]>()))
                .ReturnsAsync(new[]
                {
                    new Payment { Id = 1, UserId = 1, Amount = 100, Method = "UPI",  Status = "Completed", PaymentDate = DateTime.UtcNow, User = user, Movie = movie },
                    new Payment { Id = 2, UserId = 2, Amount = 50,  Method = "Card", Status = "Completed", PaymentDate = DateTime.UtcNow, User = user, Movie = movie }
                });

            var paged  = await _sut.GetPaymentsByUser(1, DefaultReq);
            var result = Items(paged);

            Assert.Single(result);
            Assert.Equal(100, result[0].Amount);
        }

        // ── Negative amount (refund) → Math.Abs ──────────────────

        [Fact]
        public async Task GetPaymentsByUser_NegativeAmount_ReturnedAsAbsoluteValue()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _paymentRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, object>>[]>()))
                .ReturnsAsync(new[]
                {
                    new Payment { Id = 1, UserId = 1, Amount = -90, Method = "Refund",
                                  Status = "Refunded", PaymentDate = DateTime.UtcNow,
                                  User = user, Movie = movie }
                });

            var paged  = await _sut.GetPaymentsByUser(1, DefaultReq);
            var result = Items(paged);

            Assert.Single(result);
            Assert.Equal(90, result[0].Amount); // Math.Abs applied
        }

        // ── No payments ───────────────────────────────────────────

        [Fact]
        public async Task GetPaymentsByUser_NoPayments_ReturnsEmpty()
        {
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _paymentRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, object>>[]>()))
                .ReturnsAsync(Enumerable.Empty<Payment>());

            var paged  = await _sut.GetPaymentsByUser(1, DefaultReq);
            var result = Items(paged);

            Assert.Empty(result);
            Assert.Equal(0, paged.TotalCount);
        }

        // ── Sort desc (default) ───────────────────────────────────

        [Fact]
        public async Task GetPaymentsByUser_SortDesc_NewestFirst()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var older = new Payment { Id = 1, UserId = 1, Amount = 50,  Method = "UPI",  Status = "Completed", PaymentDate = DateTime.UtcNow.AddDays(-2), User = user, Movie = movie };
            var newer = new Payment { Id = 2, UserId = 1, Amount = 100, Method = "Card", Status = "Completed", PaymentDate = DateTime.UtcNow,              User = user, Movie = movie };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _paymentRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, object>>[]>()))
                .ReturnsAsync(new[] { older, newer });

            var result = Items(await _sut.GetPaymentsByUser(1, DefaultReq));

            Assert.Equal(100, result[0].Amount); // newer first
        }

        // ── Sort asc ──────────────────────────────────────────────

        [Fact]
        public async Task GetPaymentsByUser_SortAsc_OldestFirst()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var older = new Payment { Id = 1, UserId = 1, Amount = 50,  Method = "UPI",  Status = "Completed", PaymentDate = DateTime.UtcNow.AddDays(-2), User = user, Movie = movie };
            var newer = new Payment { Id = 2, UserId = 1, Amount = 100, Method = "Card", Status = "Completed", PaymentDate = DateTime.UtcNow,              User = user, Movie = movie };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _paymentRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, object>>[]>()))
                .ReturnsAsync(new[] { newer, older });

            var req    = new GetPaymentsByUserRequestDto { PageNumber = 1, PageSize = 50, SortOrder = "asc" };
            var result = Items(await _sut.GetPaymentsByUser(1, req));

            Assert.Equal(50, result[0].Amount); // older first
        }

        // ── Pagination ────────────────────────────────────────────

        [Fact]
        public async Task GetPaymentsByUser_Pagination_ReturnsCorrectPage()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var payments = Enumerable.Range(1, 5).Select(i =>
                new Payment { Id = i, UserId = 1, Amount = i * 10, Method = "UPI",
                              Status = "Completed", PaymentDate = DateTime.UtcNow.AddDays(-i),
                              User = user, Movie = movie }).ToArray();

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _paymentRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, object>>[]>()))
                .ReturnsAsync(payments);

            var req    = new GetPaymentsByUserRequestDto { PageNumber = 2, PageSize = 2, SortOrder = "desc" };
            var paged  = await _sut.GetPaymentsByUser(1, req);
            var result = Items(paged);

            Assert.Equal(2, result.Count);
            Assert.Equal(5, paged.TotalCount);
        }

        // ── PageSize zero defaults to 50 ──────────────────────────

        [Fact]
        public async Task GetPaymentsByUser_PageSizeZero_DefaultsTo50()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _paymentRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, object>>[]>()))
                .ReturnsAsync(new[]
                {
                    new Payment { Id = 1, UserId = 1, Amount = 100, Method = "UPI",
                                  Status = "Completed", PaymentDate = DateTime.UtcNow,
                                  User = user, Movie = movie }
                });

            var req    = new GetPaymentsByUserRequestDto { PageNumber = 1, PageSize = 0, SortOrder = "desc" };
            var paged  = await _sut.GetPaymentsByUser(1, req);
            var result = Items(paged);

            Assert.Single(result);
            Assert.Equal(50, paged.PageSize); // clamped to 50
        }

        // ── Null navigation props handled gracefully ──────────────

        [Fact]
        public async Task GetPaymentsByUser_NullUserAndMovie_MapsToEmptyStrings()
        {
            var user = MakeUser();
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _paymentRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, object>>[]>()))
                .ReturnsAsync(new[]
                {
                    new Payment { Id = 1, UserId = 1, Amount = 100, Method = "UPI",
                                  Status = "Completed", PaymentDate = DateTime.UtcNow,
                                  User = null!, Movie = null! }
                });

            var paged  = await _sut.GetPaymentsByUser(1, DefaultReq);
            var result = Items(paged)[0];

            Assert.Equal(string.Empty, result.UserName);
            Assert.Equal(string.Empty, result.MovieTitle);
        }

        // ── Paged metadata is correct ─────────────────────────────

        [Fact]
        public async Task GetPaymentsByUser_PagedMetadata_IsCorrect()
        {
            var user  = MakeUser();
            var movie = MakeMovie();
            var payments = Enumerable.Range(1, 6).Select(i =>
                new Payment { Id = i, UserId = 1, Amount = i * 10, Method = "UPI",
                              Status = "Completed", PaymentDate = DateTime.UtcNow.AddDays(-i),
                              User = user, Movie = movie }).ToArray();

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _paymentRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Payment, object>>[]>()))
                .ReturnsAsync(payments);

            var req   = new GetPaymentsByUserRequestDto { PageNumber = 1, PageSize = 4, SortOrder = "desc" };
            var paged = await _sut.GetPaymentsByUser(1, req);

            Assert.Equal(6, paged.TotalCount);
            Assert.Equal(2, paged.TotalPages);
            Assert.True(paged.HasNext);
            Assert.False(paged.HasPrevious);
        }
    }
}
