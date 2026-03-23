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

        [HttpGet("{id}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetPayment(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid payment ID." });
            try
            {
                var result = await _paymentService.GetPayment(id);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetPaymentsByUser(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID." });
            try
            {
                var result = await _paymentService.GetPaymentsByUser(userId);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPayments()
        {
            try
            {
                var result = await _paymentService.GetAllPayments();
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePaymentStatus(
            int id, [FromQuery] string status)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid payment ID." });
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new { message = "Status cannot be empty." });
            try
            {
                var result = await _paymentService
                    .UpdatePaymentStatus(id, status);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex)
            { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}