using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IWishlistService
    {
        Task<WishlistResponseDto> AddToWishlist(WishlistCreateDto dto);
        Task<IEnumerable<WishlistResponseDto>> GetWishlistByUser(int userId);
        Task RemoveFromWishlist(int wishlistId);
    }
}