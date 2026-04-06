using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer,Admin,ContentManager")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        [HttpPost]
        public async Task<ActionResult> AddToWishlist(WishlistCreateDto dto)
        {
            return Ok(await _wishlistService.AddToWishlist(dto));
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetWishlist(int userId)
        {
            return Ok(await _wishlistService.GetWishlistByUser(userId));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveFromWishlist(int id)
        {
            await _wishlistService.RemoveFromWishlist(id);
            return Ok(new { message = "Item removed from wishlist." });
        }
    }
}
