using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RentalController : ControllerBase
    {
        private readonly IRentalService _rentalService;

        public RentalController(IRentalService rentalService)
        {
            _rentalService = rentalService;
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<ActionResult> RentMovie(RentalCreateDto dto)
        {
            try
            {
                var result = await _rentalService.RentMovie(dto);
                return Created($"api/Rental/{result.Id}", result);
            }
            catch { throw; }
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpPut("{id}/return")]
        public async Task<ActionResult> ReturnMovie(int id)
        {
            try
            {
                return Ok(await _rentalService.ReturnMovie(id));
            }
            catch { throw; }
        }

       
        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetRentalsByUser(
            int userId, [FromQuery] string? status = null)
        {
            try
            {
                return Ok(await _rentalService.GetRentalsByUser(userId, status));
            }
            catch { throw; }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("backfill-refunds")]
        public async Task<ActionResult> BackfillRefunds()
        {
            var count = await _rentalService.BackfillRefundedPaymentsAsync();
            return Ok(new { created = count, message = $"Backfilled {count} refund record(s)." });
        }


    }
}
