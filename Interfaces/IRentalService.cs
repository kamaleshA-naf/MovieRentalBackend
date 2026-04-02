using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IRentalService
    {
        Task<RentalResponseDto> RentMovie(RentalCreateDto dto);
        Task<RentalResponseDto> ReturnMovie(int rentalId);
        Task<IEnumerable<RentalResponseDto>> GetRentalsByUser(int userId);
        Task<bool> IsEligibleToRateAsync(int userId, int movieId); // used internally by rating controller
    }
}
