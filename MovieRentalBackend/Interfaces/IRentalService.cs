using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IRentalService
    {
        Task<RentalResponseDto> RentMovie(RentalCreateDto dto);
        Task<RentalResponseDto> ReturnMovie(int rentalId);

        // status = null → all rentals, "Active" | "Expired" | "Returned" → filtered
        Task<IEnumerable<RentalResponseDto>> GetRentalsByUser(int userId, string? status = null);

        Task<bool> IsEligibleToRateAsync(int userId, int movieId);

        // Marks all past-expiry Active rentals as Expired in the DB
        Task SyncExpiredRentalsAsync();

        // Backfills Refunded payment records for returned rentals that have none
        Task<int> BackfillRefundedPaymentsAsync();
    }
}
