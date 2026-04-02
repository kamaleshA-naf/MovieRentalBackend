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
    public class RentalController : ControllerBase
    {
        private readonly IRentalService _rentalService;

        public RentalController(IRentalService rentalService)
        {
            _rentalService = rentalService;
        }

        // POST /api/Rental
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RentMovie([FromBody] RentalCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _rentalService.RentMovie(dto)); }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // PUT /api/Rental/{id}/return
        [HttpPut("{id}/return")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> ReturnMovie(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid rental ID." });
            try { return Ok(await _rentalService.ReturnMovie(id)); }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Rental/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetRentalsByUser(int userId)
        {
            if (userId <= 0) return BadRequest(new { message = "Invalid user ID." });
            try { return Ok(await _rentalService.GetRentalsByUser(userId)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
