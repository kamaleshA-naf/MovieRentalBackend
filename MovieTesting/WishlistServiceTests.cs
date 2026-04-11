using Moq;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using MovieRentalApp.Services;

namespace MovieTesting
{
    public class WishlistServiceTests
    {
        private readonly Mock<IRepository<int, Wishlist>> _wishlistRepo = new();
        private readonly Mock<IRepository<int, Movie>> _movieRepo = new();
        private readonly Mock<IRepository<int, User>> _userRepo = new();
        private readonly WishlistService _sut;

        public WishlistServiceTests()
        {
            _sut = new WishlistService(_wishlistRepo.Object, _movieRepo.Object, _userRepo.Object);
        }

        private static User MakeUser(int id = 1) =>
            new() { UserId = id, UserName = "Alice", UserEmail = "a@a.com", Role = UserRole.Customer, IsActive = true };

        private static Movie MakeMovie(int id = 1, bool active = true) =>
            new() { Id = id, Title = "Inception", RentalPrice = 50, IsActive = active };

        // ── AddToWishlist ─────────────────────────────────────────

        [Fact]
        public async Task AddToWishlist_Valid_ReturnsDto()
        {
            var dto = new WishlistCreateDto { UserId = 1, MovieId = 1 };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _wishlistRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                         .ReturnsAsync(Enumerable.Empty<Wishlist>());
            _wishlistRepo.Setup(r => r.AddAsync(It.IsAny<Wishlist>()))
                         .ReturnsAsync(new Wishlist { Id = 1, UserId = 1, MovieId = 1, AddedDate = DateTime.UtcNow });

            var result = await _sut.AddToWishlist(dto);

            Assert.Equal("Inception", result.MovieTitle);
        }

        [Fact]
        public async Task AddToWishlist_UserNotFound_ThrowsEntityNotFoundException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.AddToWishlist(new WishlistCreateDto { UserId = 99, MovieId = 1 }));
        }

        [Fact]
        public async Task AddToWishlist_MovieNotFound_ThrowsEntityNotFoundException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Movie?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.AddToWishlist(new WishlistCreateDto { UserId = 1, MovieId = 99 }));
        }

        [Fact]
        public async Task AddToWishlist_Duplicate_ThrowsDuplicateEntityException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _wishlistRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                         .ReturnsAsync(new[] { new Wishlist { Id = 1, UserId = 1, MovieId = 1 } });

            await Assert.ThrowsAsync<DuplicateEntityException>(() =>
                _sut.AddToWishlist(new WishlistCreateDto { UserId = 1, MovieId = 1 }));
        }

        // ── GetWishlistByUser ─────────────────────────────────────

        [Fact]
        public async Task GetWishlistByUser_UserNotFound_ThrowsEntityNotFoundException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.GetWishlistByUser(99));
        }

        [Fact]
        public async Task GetWishlistByUser_ReturnsUserItems()
        {
            var movie = MakeMovie();
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _wishlistRepo.Setup(r => r.GetAllWithIncludeAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, object>>[]>()))
                         .ReturnsAsync(new[] { new Wishlist { Id = 1, UserId = 1, MovieId = 1, Movie = movie, AddedDate = DateTime.UtcNow } });

            var result = await _sut.GetWishlistByUser(1);

            Assert.Single(result);
        }

        // ── RemoveFromWishlist ────────────────────────────────────

        [Fact]
        public async Task RemoveFromWishlist_NotFound_ThrowsEntityNotFoundException()
        {
            _wishlistRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Wishlist?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.RemoveFromWishlist(99));
        }

        [Fact]
        public async Task RemoveFromWishlist_Valid_CallsDelete()
        {
            _wishlistRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Wishlist { Id = 1 });
            _wishlistRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            await _sut.RemoveFromWishlist(1); // no exception = pass
        }

        // ── RemoveByUserAndMovieAsync ─────────────────────────────

        [Fact]
        public async Task RemoveByUserAndMovieAsync_NotFound_NoException()
        {
            _wishlistRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                         .ReturnsAsync(Enumerable.Empty<Wishlist>());

            await _sut.RemoveByUserAndMovieAsync(1, 1); // safe no-op
        }
    }
}
