using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IGenreService
    {
        Task<GenreResponseDto> AddGenre(GenreCreateDto dto);
        Task<IEnumerable<GenreResponseDto>> GetAllGenres();
    }
}
