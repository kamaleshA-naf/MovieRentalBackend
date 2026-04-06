using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<ActionResult> AddToCart(CartAddDto dto)
        {
            return Ok(await _cartService.AddToCart(dto));
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetCart(int userId)
        {
            return Ok(await _cartService.GetCartByUser(userId));
        }

        [Authorize(Roles = "Customer")]
        [HttpPut("{id}/duration")]
        public async Task<ActionResult> UpdateDuration(int id, CartUpdateDto dto)
        {
            return Ok(await _cartService.UpdateDuration(id, dto));
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("checkout")]
        public async Task<ActionResult> Checkout(CartCheckoutDto dto)
        {
            return Ok(await _cartService.Checkout(dto));
        }

        [Authorize(Roles = "Customer")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveFromCart(int id)
        {
            await _cartService.RemoveFromCart(id);
            return Ok(new { message = "Item removed from cart." });
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpDelete("clear/{userId}")]
        public async Task<ActionResult> ClearCart(int userId)
        {
            await _cartService.ClearCart(userId);
            return Ok(new { message = "Cart cleared." });
        }
    }
}
