using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IMovieRatingService
    {
        Task<MovieRatingResponseDto> RateMovie(int movieId, MovieRatingCreateDto dto);
        Task<MovieRatingResponseDto> RemoveRating(int movieId, int userId);
        Task<MovieRatingSummaryDto> GetMovieRatingSummary(int movieId);
        Task<MovieRatingResponseDto?> GetUserRatingForMovie(int movieId, int userId);
        Task<IEnumerable<MovieRatingResponseDto>> GetUserRatings(int userId);
        Task<UserGenrePreferenceDto> GetUserGenrePreferences(int userId);
    }
}