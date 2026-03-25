using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IMovieService
    {
        Task<MovieResponseDto> AddMovie(MovieCreateDto dto);
        Task<MovieResponseDto> GetMovie(int id);
        Task<PagedResultDto<MovieResponseDto>> GetAllMovies(PaginationDto pagination);
        Task<PagedResultDto<MovieResponseDto>> SearchMovies(string keyword, PaginationDto pagination);
        Task<PagedResultDto<MovieResponseDto>> GetMoviesByGenre(int genreId, PaginationDto pagination);
        Task<IEnumerable<MovieResponseDto>> GetTrendingMovies(List<int> movieIds);
        Task<MovieResponseDto> UpdateMovie(int id, MovieUpdateDto dto);
        Task<MovieResponseDto> DeleteMovie(int id);
        Task<bool> IncrementViewCountAsync(int id);
    }
}