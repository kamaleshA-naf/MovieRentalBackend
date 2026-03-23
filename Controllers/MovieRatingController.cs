using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [ApiController]
    [Route("api/Movie")]
    [Authorize]
    public class MovieRatingController : ControllerBase
    {
        private readonly IMovieRatingService _ratingService;

        public MovieRatingController(IMovieRatingService ratingService)
        {
            _ratingService = ratingService;
        }

        // POST /api/Movie/{id}/rate
        // Send same value again → removes the rating (toggle)
        // Send different value → updates the rating
        [HttpPost("{id}/rate")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RateMovie(
            int id, [FromBody] MovieRatingCreateDto dto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _ratingService.RateMovie(id, dto);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex)
            { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        // DELETE /api/Movie/{id}/rate/{userId}
        [HttpDelete("{id}/rate/{userId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RemoveRating(int id, int userId)
        {
            if (id <= 0 || userId <= 0)
                return BadRequest(new { message = "Invalid ID." });

            try
            {
                var result = await _ratingService.RemoveRating(id, userId);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Movie/{id}/ratings
        [HttpGet("{id}/ratings")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRatingSummary(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

            try
            {
                var result = await _ratingService.GetMovieRatingSummary(id);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Movie/{id}/rating/user/{userId}
        [HttpGet("{id}/rating/user/{userId}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetUserRating(int id, int userId)
        {
            if (id <= 0 || userId <= 0)
                return BadRequest(new { message = "Invalid ID." });

            try
            {
                var result = await _ratingService
                    .GetUserRatingForMovie(id, userId);

                // Return 0 if not rated — not a 404
                return Ok(result ?? new MovieRatingResponseDto
                {
                    MovieId = id,
                    UserId = userId,
                    RatingValue = 0,
                    RatingLabel = "Not rated",
                    IsRemoved = false
                });
            }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Movie/user/{userId}/ratings
        [HttpGet("user/{userId}/ratings")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetUserRatings(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID." });

            try
            {
                var result = await _ratingService.GetUserRatings(userId);
                return Ok(result);
            }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}