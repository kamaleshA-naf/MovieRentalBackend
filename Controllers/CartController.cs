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
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToCart(
            [FromBody] CartAddDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _cartService.AddToCart(dto);
                return Ok(result);
            }
            catch (DuplicateEntityException ex)
            { return Conflict(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex)
            { return BadRequest(new { message = ex.Message }); }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetCart(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID." });
            try
            {
                var result = await _cartService.GetCartByUser(userId);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid cart item ID." });
            try
            {
                await _cartService.RemoveFromCart(id);
                return Ok(new { message = "Item removed from cart." });
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("{id}/duration")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UpdateDuration(
            int id, [FromBody] CartUpdateDto dto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid cart item ID." });
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _cartService.UpdateDuration(id, dto);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex)
            { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("checkout")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Checkout(
            [FromBody] CartCheckoutDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _cartService.Checkout(dto);
                return Ok(result);
            }
            catch (BusinessRuleViolationException ex)
            { return BadRequest(new { message = ex.Message }); }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("clear/{userId}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID." });
            try
            {
                await _cartService.ClearCart(userId);
                return Ok(new { message = "Cart cleared." });
            }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAnalytics()
        {
            try
            {
                var result = await _cartService.GetAnalytics();
                return Ok(result);
            }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}