using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

       
        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetPaymentsByUser(
            int userId, [FromQuery] GetPaymentsByUserRequestDto request)
        {
            try
            {
                return Ok(await _paymentService.GetPaymentsByUser(userId, request));
            }
            catch { throw; }
        }
    }
}
