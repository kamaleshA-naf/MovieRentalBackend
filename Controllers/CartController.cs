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

        // POST /api/Cart
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToCart([FromBody] CartAddDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _cartService.AddToCart(dto)); }
            catch (DuplicateEntityException ex) { return Conflict(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Cart/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetCart(int userId)
        {
            if (userId <= 0) return BadRequest(new { message = "Invalid user ID." });
            try { return Ok(await _cartService.GetCartByUser(userId)); }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // DELETE /api/Cart/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid cart item ID." });
            try { await _cartService.RemoveFromCart(id); return Ok(new { message = "Item removed from cart." }); }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // PUT /api/Cart/{id}/duration
        [HttpPut("{id}/duration")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UpdateDuration(int id, [FromBody] CartUpdateDto dto)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid cart item ID." });
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _cartService.UpdateDuration(id, dto)); }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST /api/Cart/checkout
        [HttpPost("checkout")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Checkout([FromBody] CartCheckoutDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _cartService.Checkout(dto)); }
            catch (BusinessRuleViolationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // DELETE /api/Cart/clear/{userId}
        [HttpDelete("clear/{userId}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            if (userId <= 0) return BadRequest(new { message = "Invalid user ID." });
            try { await _cartService.ClearCart(userId); return Ok(new { message = "Cart cleared." }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // TODO: remove or implement — GET /api/Cart/analytics is called by cart.service.ts
        // (getAnalytics method) but this endpoint does NOT exist in the backend.
    }
}
