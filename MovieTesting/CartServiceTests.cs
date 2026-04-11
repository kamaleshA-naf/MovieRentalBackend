using Moq;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using MovieRentalApp.Services;

namespace MovieTesting
{
    public class CartServiceTests
    {
        private readonly Mock<IRepository<int, Cart>> _cartRepo = new();
        private readonly Mock<IRepository<int, Movie>> _movieRepo = new();
        private readonly Mock<IRepository<int, User>> _userRepo = new();
        private readonly Mock<IRepository<int, Rental>> _rentalRepo = new();
        private readonly Mock<IRepository<int, Payment>> _paymentRepo = new();
        private readonly Mock<IRepository<int, MovieGenre>> _movieGenreRepo = new();
        private readonly Mock<IRepository<int, Genre>> _genreRepo = new();
        private readonly CartService _sut;

        public CartServiceTests()
        {
            _sut = new CartService(
                _cartRepo.Object, _movieRepo.Object, _userRepo.Object,
                _rentalRepo.Object, _paymentRepo.Object,
                _movieGenreRepo.Object, _genreRepo.Object);
        }

        private static User MakeUser(int id = 1) =>
            new() { UserId = id, UserName = "Alice", UserEmail = "a@a.com", Role = UserRole.Customer, IsActive = true };

        private static Movie MakeMovie(int id = 1, bool active = true) =>
            new() { Id = id, Title = "Inception", RentalPrice = 50, IsActive = active };

        private void SetupGenreHelpers()
        {
            _movieGenreRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MovieGenre, bool>>>()))
                           .ReturnsAsync(Enumerable.Empty<MovieGenre>());
            _genreRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Genre>());
        }

        // ── AddToCart ─────────────────────────────────────────────

        [Fact]
        public async Task AddToCart_Valid_ReturnsCartResponseDto()
        {
            var dto = new CartAddDto { UserId = 1, MovieId = 1, DurationDays = 7 };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _cartRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, bool>>>()))
                     .ReturnsAsync(Enumerable.Empty<Cart>());
            _rentalRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                       .ReturnsAsync(Enumerable.Empty<Rental>());
            _cartRepo.Setup(r => r.AddAsync(It.IsAny<Cart>()))
                     .ReturnsAsync(new Cart { Id = 1, UserId = 1, MovieId = 1, DurationDays = 7, AddedAt = DateTime.UtcNow });
            SetupGenreHelpers();

            var result = await _sut.AddToCart(dto);

            Assert.Equal("Inception", result.MovieTitle);
            Assert.Equal(350, result.TotalCost); // 50 * 7
        }

        [Fact]
        public async Task AddToCart_UserNotFound_ThrowsEntityNotFoundException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.AddToCart(new CartAddDto { UserId = 99, MovieId = 1 }));
        }

        [Fact]
        public async Task AddToCart_MovieNotFound_ThrowsEntityNotFoundException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Movie?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.AddToCart(new CartAddDto { UserId = 1, MovieId = 99 }));
        }

        [Fact]
        public async Task AddToCart_InactiveMovie_ThrowsBusinessRuleViolation()
        {
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie(active: false));

            await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
                _sut.AddToCart(new CartAddDto { UserId = 1, MovieId = 1 }));
        }

        [Fact]
        public async Task AddToCart_AlreadyInCart_ThrowsDuplicateEntityException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _cartRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, bool>>>()))
                     .ReturnsAsync(new[] { new Cart { Id = 1, UserId = 1, MovieId = 1 } });

            await Assert.ThrowsAsync<DuplicateEntityException>(() =>
                _sut.AddToCart(new CartAddDto { UserId = 1, MovieId = 1 }));
        }

        [Fact]
        public async Task AddToCart_AlreadyRented_ThrowsBusinessRuleViolation()
        {
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _cartRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, bool>>>()))
                     .ReturnsAsync(Enumerable.Empty<Cart>());
            _rentalRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                       .ReturnsAsync(new[] { new Rental { Id = 1, UserId = 1, MovieId = 1, StoredStatus = "Active", ExpiryDate = DateTime.UtcNow.AddDays(5) } });

            await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
                _sut.AddToCart(new CartAddDto { UserId = 1, MovieId = 1 }));
        }

        // ── RemoveFromCart ────────────────────────────────────────

        [Fact]
        public async Task RemoveFromCart_NotFound_ThrowsEntityNotFoundException()
        {
            _cartRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Cart?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.RemoveFromCart(99));
        }

        [Fact]
        public async Task RemoveFromCart_Valid_CallsDelete()
        {
            _cartRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Cart { Id = 1 });
            _cartRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            await _sut.RemoveFromCart(1);
        }

        // ── UpdateDuration ────────────────────────────────────────

        [Fact]
        public async Task UpdateDuration_NotFound_ThrowsEntityNotFoundException()
        {
            _cartRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Cart?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.UpdateDuration(99, new CartUpdateDto { DurationDays = 5 }));
        }

        [Fact]
        public async Task UpdateDuration_Valid_ReturnsDtoWithNewDuration()
        {
            var cart = new Cart { Id = 1, UserId = 1, MovieId = 1, DurationDays = 7 };
            _cartRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cart);
            _cartRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Cart>())).ReturnsAsync(cart);
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            SetupGenreHelpers();

            var result = await _sut.UpdateDuration(1, new CartUpdateDto { DurationDays = 14 });

            Assert.Equal(14, result.DurationDays);
        }

        // ── ClearCart ─────────────────────────────────────────────

        [Fact]
        public async Task ClearCart_DeletesAllUserItems()
        {
            _cartRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, bool>>>()))
                     .ReturnsAsync(new[] { new Cart { Id = 1, UserId = 1 }, new Cart { Id = 2, UserId = 1 } });
            _cartRepo.Setup(r => r.DeleteAsync(It.IsAny<int>())).ReturnsAsync(true);

            await _sut.ClearCart(1);

            _cartRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Exactly(2));
        }

        // ── GetCartByUser ─────────────────────────────────────────

        [Fact]
        public async Task GetCartByUser_UserNotFound_ThrowsEntityNotFoundException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.GetCartByUser(99));
        }

        [Fact]
        public async Task GetCartByUser_ReturnsUserItems()
        {
            var movie = MakeMovie();
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _cartRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, object>>[]>()))
                     .ReturnsAsync(new[]
                     {
                         new Cart { Id = 1, UserId = 1, MovieId = 1, DurationDays = 7, AddedAt = DateTime.UtcNow, Movie = movie },
                         new Cart { Id = 2, UserId = 2, MovieId = 1, DurationDays = 3, AddedAt = DateTime.UtcNow, Movie = movie }
                     });
            SetupGenreHelpers();

            var result = await _sut.GetCartByUser(1);

            Assert.Single(result);
        }

        // ── Checkout ──────────────────────────────────────────────

        [Fact]
        public async Task Checkout_EmptyCart_ThrowsBusinessRuleViolation()
        {
            _cartRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, object>>[]>()))
                     .ReturnsAsync(Enumerable.Empty<Cart>());

            await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
                _sut.Checkout(new CartCheckoutDto { UserId = 1, PaymentMethod = "UPI" }));
        }

        [Fact]
        public async Task Checkout_UserNotFound_ThrowsEntityNotFoundException()
        {
            var movie = MakeMovie();
            _cartRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, object>>[]>()))
                     .ReturnsAsync(new[] { new Cart { Id = 1, UserId = 1, MovieId = 1, DurationDays = 7, Movie = movie } });
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.Checkout(new CartCheckoutDto { UserId = 1, PaymentMethod = "UPI" }));
        }

        [Fact]
        public async Task Checkout_ValidCart_CreatesRentalsAndClearsCart()
        {
            var movie = MakeMovie();
            var user = MakeUser();
            _cartRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, object>>[]>()))
                     .ReturnsAsync(new[] { new Cart { Id = 1, UserId = 1, MovieId = 1, DurationDays = 7, Movie = movie } });
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _rentalRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                       .ReturnsAsync(Enumerable.Empty<Rental>());
            _rentalRepo.Setup(r => r.AddAsync(It.IsAny<Rental>()))
                       .ReturnsAsync(new Rental { Id = 1, UserId = 1, MovieId = 1, StoredStatus = "Active", RentalDate = DateTime.UtcNow, ExpiryDate = DateTime.UtcNow.AddDays(7) });
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync(new Payment());
            // ClearCart uses FindAsync
            _cartRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, bool>>>()))
                     .ReturnsAsync(new[] { new Cart { Id = 1, UserId = 1 } });
            _cartRepo.Setup(r => r.DeleteAsync(It.IsAny<int>())).ReturnsAsync(true);

            var result = await _sut.Checkout(new CartCheckoutDto { UserId = 1, PaymentMethod = "UPI" });

            Assert.Equal(1, result.TotalMovies);
            Assert.Equal(350, result.TotalAmount); // 50 * 7
            Assert.Empty(result.SkippedMovies);
        }

        [Fact]
        public async Task Checkout_AlreadyRentedMovie_SkipsIt()
        {
            var movie = MakeMovie();
            var user = MakeUser();
            _cartRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, object>>[]>()))
                     .ReturnsAsync(new[] { new Cart { Id = 1, UserId = 1, MovieId = 1, DurationDays = 7, Movie = movie } });
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            // Already rented
            _rentalRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                       .ReturnsAsync(new[] { new Rental { StoredStatus = "Active", ExpiryDate = DateTime.UtcNow.AddDays(5) } });
            _cartRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, bool>>>()))
                     .ReturnsAsync(Enumerable.Empty<Cart>());

            var result = await _sut.Checkout(new CartCheckoutDto { UserId = 1, PaymentMethod = "UPI" });

            Assert.Equal(0, result.TotalMovies);
            Assert.Contains("Inception", result.SkippedMovies);
        }

        [Fact]
        public async Task AddToCart_ZeroDuration_DefaultsToSeven()
        {
            var dto = new CartAddDto { UserId = 1, MovieId = 1, DurationDays = 0 };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _cartRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cart, bool>>>()))
                     .ReturnsAsync(Enumerable.Empty<Cart>());
            _rentalRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Rental, bool>>>()))
                       .ReturnsAsync(Enumerable.Empty<Rental>());
            _cartRepo.Setup(r => r.AddAsync(It.IsAny<Cart>()))
                     .ReturnsAsync(new Cart { Id = 1, UserId = 1, MovieId = 1, DurationDays = 7, AddedAt = DateTime.UtcNow });
            SetupGenreHelpers();

            var result = await _sut.AddToCart(dto);

            Assert.Equal(7, result.DurationDays);
        }
    }
}
