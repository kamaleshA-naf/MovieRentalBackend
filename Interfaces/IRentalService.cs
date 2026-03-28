using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IRentalService
    {
        Task<RentalResponseDto> RentMovie(RentalCreateDto dto);
        Task<RentalResponseDto> ReturnMovie(int rentalId);
        Task<RentalResponseDto> GetRental(int id);
        Task<IEnumerable<RentalResponseDto>> GetRentalsByUser(int userId);
        Task<IEnumerable<RentalResponseDto>> GetActiveRentals();
        Task<bool> IsEligibleToRateAsync(int userId, int movieId);
    }
}