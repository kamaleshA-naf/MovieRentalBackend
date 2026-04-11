using Moq;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using MovieRentalApp.Services;

namespace MovieTesting
{
    public class GenreServiceTests
    {
        private readonly Mock<IRepository<int, Genre>> _genreRepo = new();
        private readonly GenreService _sut;

        public GenreServiceTests()
        {
            _sut = new GenreService(_genreRepo.Object);
        }

        [Fact]
        public async Task AddGenre_ValidName_ReturnsDto()
        {
            var dto = new GenreCreateDto { Name = "Action" };
            _genreRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Genre, bool>>>()))
                      .ReturnsAsync(Enumerable.Empty<Genre>());
            _genreRepo.Setup(r => r.AddAsync(It.IsAny<Genre>()))
                      .ReturnsAsync(new Genre { Id = 1, Name = "Action" });

            var result = await _sut.AddGenre(dto);

            Assert.Equal("Action", result.Name);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task AddGenre_EmptyName_ThrowsBusinessRuleViolation()
        {
            var dto = new GenreCreateDto { Name = "   " };
            await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _sut.AddGenre(dto));
        }

        [Fact]
        public async Task AddGenre_DuplicateName_ThrowsDuplicateEntityException()
        {
            var dto = new GenreCreateDto { Name = "Action" };
            _genreRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Genre, bool>>>()))
                      .ReturnsAsync(new[] { new Genre { Id = 1, Name = "Action" } });

            await Assert.ThrowsAsync<DuplicateEntityException>(() => _sut.AddGenre(dto));
        }

        [Fact]
        public async Task GetAllGenres_ReturnsAllGenres()
        {
            _genreRepo.Setup(r => r.GetAllAsync())
                      .ReturnsAsync(new[] { new Genre { Id = 1, Name = "Action" }, new Genre { Id = 2, Name = "Drama" } });

            var result = await _sut.GetAllGenres();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAllGenres_EmptyRepo_ReturnsEmpty()
        {
            _genreRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Genre>());

            var result = await _sut.GetAllGenres();

            Assert.Empty(result);
        }

        [Fact]
        public async Task AddGenre_TrimsAndChecksCase_DuplicateDifferentCase_ThrowsDuplicate()
        {
            var dto = new GenreCreateDto { Name = "ACTION" };
            _genreRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Genre, bool>>>()))
                      .ReturnsAsync(new[] { new Genre { Id = 1, Name = "action" } });

            await Assert.ThrowsAsync<DuplicateEntityException>(() => _sut.AddGenre(dto));
        }
    }
}
