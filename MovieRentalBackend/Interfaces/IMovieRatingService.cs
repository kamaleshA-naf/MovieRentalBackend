using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IMovieRatingService
    {
        Task<MovieRatingResponseDto>                    RateMovie(int movieId, int userId, MovieRatingCreateDto dto);
        Task<MovieRatingSummaryDto>                     GetMovieRatingSummary(int movieId);
        Task<PagedResultDto<MovieRatingResponseDto>>    GetMovieRatingsPaginatedAsync(int movieId, GetMovieRatingsRequestDto request);
        Task<MovieRatingResponseDto?>                   GetUserRatingForMovie(int movieId, int userId);
    }
}
