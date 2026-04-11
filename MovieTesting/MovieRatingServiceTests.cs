using Moq;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using MovieRentalApp.Services;

namespace MovieTesting
{
    public class MovieRatingServiceTests
    {
        private readonly Mock<IRepository<int, MovieRating>> _ratingRepo = new();
        private readonly Mock<IRepository<int, Movie>> _movieRepo = new();
        private readonly Mock<IRepository<int, User>> _userRepo = new();
        private readonly AuditLogService _auditLog = new FakeAuditLogService();
        private readonly MovieRatingService _sut;

        public MovieRatingServiceTests()
        {
            // MovieRatingService also uses MovieContext for paginated queries.
            // We test the non-EF paths (RateMovie, GetMovieRatingSummary, GetUserRatingForMovie).
            _sut = new MovieRatingService(
                _ratingRepo.Object, _movieRepo.Object, _userRepo.Object,
                _auditLog, null!);
        }

        private static User MakeUser(int id = 1) =>
            new() { UserId = id, UserName = "Alice", UserEmail = "a@a.com", Role = UserRole.Customer };

        private static Movie MakeMovie(int id = 1) =>
            new() { Id = id, Title = "Inception", RentalPrice = 50, IsActive = true };

        // ── RateMovie ─────────────────────────────────────────────

        [Fact]
        public async Task RateMovie_NewRating_ReturnsDto()
        {
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _ratingRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MovieRating, bool>>>()))
                       .ReturnsAsync(Enumerable.Empty<MovieRating>());
            var added = new MovieRating { Id = 1, MovieId = 1, UserId = 1, RatingValue = 3, RatedAt = DateTime.UtcNow };
            _ratingRepo.Setup(r => r.AddAsync(It.IsAny<MovieRating>())).ReturnsAsync(added);
            _movieRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Movie>())).ReturnsAsync(MakeMovie());

            var result = await _sut.RateMovie(1, 1, new MovieRatingCreateDto { RatingValue = 3 });

            Assert.Equal(3, result.RatingValue);
            Assert.Equal("Love this!", result.RatingLabel);
        }

        [Fact]
        public async Task RateMovie_MovieNotFound_ThrowsEntityNotFoundException()
        {
            _movieRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Movie?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.RateMovie(99, 1, new MovieRatingCreateDto { RatingValue = 2 }));
        }

        [Fact]
        public async Task RateMovie_UserNotFound_ThrowsEntityNotFoundException()
        {
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.RateMovie(1, 99, new MovieRatingCreateDto { RatingValue = 2 }));
        }

        [Fact]
        public async Task RateMovie_SameValueAgain_RemovesRating()
        {
            var existing = new MovieRating { Id = 1, MovieId = 1, UserId = 1, RatingValue = 2, IsRemoved = false };
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _ratingRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MovieRating, bool>>>()))
                       .ReturnsAsync(new[] { existing });
            _ratingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<MovieRating>())).ReturnsAsync(existing);
            _movieRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Movie>())).ReturnsAsync(MakeMovie());

            var result = await _sut.RateMovie(1, 1, new MovieRatingCreateDto { RatingValue = 2 });

            Assert.True(result.IsRemoved);
            Assert.Equal(0, result.RatingValue);
        }

        [Fact]
        public async Task RateMovie_DifferentValue_UpdatesRating()
        {
            var existing = new MovieRating { Id = 1, MovieId = 1, UserId = 1, RatingValue = 1, IsRemoved = false };
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());
            _ratingRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MovieRating, bool>>>()))
                       .ReturnsAsync(new[] { existing });
            _ratingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<MovieRating>())).ReturnsAsync(existing);
            _movieRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Movie>())).ReturnsAsync(MakeMovie());

            var result = await _sut.RateMovie(1, 1, new MovieRatingCreateDto { RatingValue = 3 });

            Assert.Equal(3, result.RatingValue);
            Assert.False(result.IsRemoved);
        }

        // ── GetMovieRatingSummary ─────────────────────────────────

        [Fact]
        public async Task GetMovieRatingSummary_MovieNotFound_ThrowsEntityNotFoundException()
        {
            _movieRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Movie?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.GetMovieRatingSummary(99));
        }

        [Fact]
        public async Task GetMovieRatingSummary_NoRatings_ReturnsZeroAverage()
        {
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _ratingRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MovieRating, bool>>>()))
                       .ReturnsAsync(Enumerable.Empty<MovieRating>());

            var result = await _sut.GetMovieRatingSummary(1);

            Assert.Equal(0, result.AverageRating);
            Assert.Equal(0, result.TotalRatings);
        }

        [Fact]
        public async Task GetMovieRatingSummary_WithRatings_ReturnsCorrectCounts()
        {
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _ratingRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MovieRating, bool>>>()))
                       .ReturnsAsync(new[]
                       {
                           new MovieRating { RatingValue = 1, IsRemoved = false },
                           new MovieRating { RatingValue = 2, IsRemoved = false },
                           new MovieRating { RatingValue = 3, IsRemoved = false }
                       });

            var result = await _sut.GetMovieRatingSummary(1);

            Assert.Equal(3, result.TotalRatings);
            Assert.Equal(1, result.NotForMeCount);
            Assert.Equal(1, result.LikeCount);
            Assert.Equal(1, result.LoveCount);
            Assert.Equal(2.0, result.AverageRating);
        }

        // ── GetUserRatingForMovie ─────────────────────────────────

        [Fact]
        public async Task GetUserRatingForMovie_NoRating_ReturnsNull()
        {
            _ratingRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MovieRating, bool>>>()))
                       .ReturnsAsync(Enumerable.Empty<MovieRating>());

            var result = await _sut.GetUserRatingForMovie(1, 1);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserRatingForMovie_HasRating_ReturnsDto()
        {
            var rating = new MovieRating { Id = 1, MovieId = 1, UserId = 1, RatingValue = 2, IsRemoved = false, RatedAt = DateTime.UtcNow };
            _ratingRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MovieRating, bool>>>()))
                       .ReturnsAsync(new[] { rating });
            _movieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeMovie());
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeUser());

            var result = await _sut.GetUserRatingForMovie(1, 1);

            Assert.NotNull(result);
            Assert.Equal(2, result!.RatingValue);
            Assert.Equal("I like this", result.RatingLabel);
        }
    }
}
