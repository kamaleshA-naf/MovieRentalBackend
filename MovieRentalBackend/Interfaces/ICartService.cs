using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface ICartService
    {
        Task<CartResponseDto> AddToCart(CartAddDto dto);
        Task<IEnumerable<CartResponseDto>> GetCartByUser(int userId);
        Task RemoveFromCart(int cartId);
        Task<CartResponseDto> UpdateDuration(int cartId, CartUpdateDto dto);
        Task<CartCheckoutResultDto> Checkout(CartCheckoutDto dto);
        Task ClearCart(int userId);
    }
}
