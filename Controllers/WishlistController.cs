using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        [HttpPost]
        [Authorize(Roles = "Customer,Admin,ContentManager")]
        public async Task<IActionResult> AddToWishlist(
            [FromBody] WishlistCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _wishlistService.AddToWishlist(dto);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (DuplicateEntityException ex)
            { return Conflict(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Customer,Admin,ContentManager")]
        public async Task<IActionResult> GetWishlist(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID." });
            try
            {
                var result = await _wishlistService.GetWishlistByUser(userId);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Customer,Admin,ContentManager")]
        public async Task<IActionResult> RemoveFromWishlist(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid wishlist item ID." });
            try
            {
                await _wishlistService.RemoveFromWishlist(id);
                return Ok(new { message = "Item removed from wishlist." });
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}