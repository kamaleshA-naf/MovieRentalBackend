using Microsoft.AspNetCore.Http;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IMovieService
    {
        Task<MovieResponseDto>                 AddMovie(MovieCreateDto request);
        Task<MovieResponseDto>                 GetMovie(int id);
        Task<PagedResultDto<MovieResponseDto>> GetMovies(GetMoviesRequestDto request);
        Task<IEnumerable<MovieResponseDto>>    GetTrendingMovies(int top = 10);
        Task<MovieResponseDto>                 UpdateMovie(int id, MovieUpdateDto request);
        Task<MovieResponseDto>                 DeleteMovie(int id);
        Task<bool>                             IncrementViewCountAsync(int id);
        Task<string>                           UploadVideoAsync(int movieId, IFormFile file, string webRootPath);
        Task<string>                           UploadThumbnailAsync(int movieId, IFormFile file, string webRootPath);
    }
}
