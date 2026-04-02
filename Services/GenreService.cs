using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Services
{
    public class GenreService : IGenreService
    {
        private readonly IRepository<int, Genre> _genreRepository;

        public GenreService(IRepository<int, Genre> genreRepository)
        {
            _genreRepository = genreRepository;
        }

        public async Task<GenreResponseDto> AddGenre(GenreCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new BusinessRuleViolationException(
                    "Genre name cannot be empty.");

            var existing = await _genreRepository
                .FindAsync(g => g.Name.ToLower() == dto.Name.ToLower());
            if (existing.Any())
                throw new DuplicateEntityException(
                    $"Genre '{dto.Name}' already exists.");

            var genre = new Genre { Name = dto.Name };
            var created = await _genreRepository.AddAsync(genre);
            return new GenreResponseDto { Id = created.Id, Name = created.Name };
        }

        public async Task<IEnumerable<GenreResponseDto>> GetAllGenres()
        {
            var genres = await _genreRepository.GetAllAsync();
            return genres.Select(g => new GenreResponseDto { Id = g.Id, Name = g.Name });
        }
    }
}