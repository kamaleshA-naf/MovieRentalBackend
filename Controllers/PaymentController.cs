using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;

namespace MovieRentalApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // GET /api/Payment/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetPaymentsByUser(int userId)
        {
            if (userId <= 0) return BadRequest(new { message = "Invalid user ID." });
            try { return Ok(await _paymentService.GetPaymentsByUser(userId)); }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
