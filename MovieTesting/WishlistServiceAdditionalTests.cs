using Moq;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Services;

namespace MovieTesting
{
    /// <summary>
    /// Additional coverage for WishlistService paths.
    /// </summary>
    public class WishlistServiceAdditionalTests
    {
        private readonly Mock<IRepository<int, Wishlist>> _wishlistRepo = new();
        private readonly Mock<IRepository<int, Movie>>    _movieRepo    = new();
        private readonly Mock<IRepository<int, User>>     _userRepo     = new();
        private readonly WishlistService _sut;

        public WishlistServiceAdditionalTests()
        {
            _sut = new WishlistService(_wishlistRepo.Object, _movieRepo.Object, _userRepo.Object);
        }

        private static User  MakeUser(int id = 1)  => new() { UserId = id, UserName = "Alice", UserEmail = "a@a.com", Role = UserRole.Customer };
        private static Movie MakeMovie(int id = 1) => new() { Id = id, Title = "Inception", RentalPrice = 50, ThumbnailUrl = "/img/inception.jpg" };

        // ── GetWishlistByUser: filters other users' items ─────────

        [Fact]
        public async Task GetWishlistByUser_FiltersOtherUsersItems()
        {
            var movie = MakeMovie();
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _wishlistRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, object>>[]>()))
                .ReturnsAsync(new[]
                {
                    new Wishlist { Id = 1, UserId = 1, MovieId = 1, Movie = movie, AddedDate = DateTime.UtcNow },
                    new Wishlist { Id = 2, UserId = 2, MovieId = 1, Movie = movie, AddedDate = DateTime.UtcNow }
                });

            var result = await _sut.GetWishlistByUser(1);

            Assert.Single(result);
        }

        // ── GetWishlistByUser: maps ThumbnailUrl ──────────────────

        [Fact]
        public async Task GetWishlistByUser_MapsThumbnailUrl()
        {
            var movie = MakeMovie();
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _wishlistRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, object>>[]>()))
                .ReturnsAsync(new[]
                {
                    new Wishlist { Id = 1, UserId = 1, MovieId = 1, Movie = movie, AddedDate = DateTime.UtcNow }
                });

            var result = (await _sut.GetWishlistByUser(1)).First();

            Assert.Equal("/img/inception.jpg", result.ThumbnailUrl);
        }

        // ── RemoveByUserAndMovieAsync: found → deletes ────────────

        [Fact]
        public async Task RemoveByUserAndMovieAsync_Found_DeletesItem()
        {
            _wishlistRepo.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                .ReturnsAsync(new[] { new Wishlist { Id = 5, UserId = 1, MovieId = 1 } });
            _wishlistRepo.Setup(r => r.DeleteAsync(5)).ReturnsAsync(true);

            await _sut.RemoveByUserAndMovieAsync(1, 1);

            _wishlistRepo.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        // ── GetWishlistByUser: empty list ─────────────────────────

        [Fact]
        public async Task GetWishlistByUser_Empty_ReturnsEmpty()
        {
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _wishlistRepo.Setup(r => r.GetAllWithIncludeAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, object>>[]>()))
                .ReturnsAsync(Enumerable.Empty<Wishlist>());

            var result = await _sut.GetWishlistByUser(1);

            Assert.Empty(result);
        }

        // ── AddToWishlist: maps RentalPrice ───────────────────────

        [Fact]
        public async Task AddToWishlist_MapsRentalPrice()
        {
            var movie = MakeMovie();
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(movie);
            _wishlistRepo.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                .ReturnsAsync(Enumerable.Empty<Wishlist>());
            _wishlistRepo.Setup(r => r.AddAsync(It.IsAny<Wishlist>()))
                .ReturnsAsync(new Wishlist { Id = 1, UserId = 1, MovieId = 1, AddedDate = DateTime.UtcNow });

            var result = await _sut.AddToWishlist(new MovieRentalApp.Models.DTOs.WishlistCreateDto { UserId = 1, MovieId = 1 });

            Assert.Equal(50, result.RentalPrice);
        }
    }
}
