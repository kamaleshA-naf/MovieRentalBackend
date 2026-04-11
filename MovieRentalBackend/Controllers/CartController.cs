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
            try
            {
                return Ok(await _cartService.AddToCart(dto));
            }
            catch { throw; }
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetCart(int userId)
        {
            try
            {
                return Ok(await _cartService.GetCartByUser(userId));
            }
            catch { throw; }
        }

        [Authorize(Roles = "Customer")]
        [HttpPut("{id}/duration")]
        public async Task<ActionResult> UpdateDuration(int id, CartUpdateDto dto)
        {
            try
            {
                return Ok(await _cartService.UpdateDuration(id, dto));
            }
            catch { throw; }
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("checkout")]
        public async Task<ActionResult> Checkout(CartCheckoutDto dto)
        {
            try
            {
                return Ok(await _cartService.Checkout(dto));
            }
            catch { throw; }
        }

        [Authorize(Roles = "Customer")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveFromCart(int id)
        {
            try
            {
                await _cartService.RemoveFromCart(id);
                return Ok(new { message = "Item removed from cart." });
            }
            catch { throw; }
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpDelete("clear/{userId}")]
        public async Task<ActionResult> ClearCart(int userId)
        {
            try
            {
                await _cartService.ClearCart(userId);
                return Ok(new { message = "Cart cleared." });
            }
            catch { throw; }
        }
    }
}
