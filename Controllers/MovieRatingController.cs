using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;
using System.Security.Claims;

namespace MovieRentalApp.Controllers
{
    [ApiController]
    [Route("api/Movie")]
    [Authorize]
    public class MovieRatingController : ControllerBase
    {
        private readonly IMovieRatingService _ratingService;
        private readonly IRentalService _rentalService;

        public MovieRatingController(
            IMovieRatingService ratingService,
            IRentalService rentalService)
        {
            _ratingService = ratingService;
            _rentalService = rentalService;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        // POST /api/Movie/{id}/rate
        // Body: { "ratingValue": 1|2|3 }
        [HttpPost("{id}/rate")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RateMovie(
            int id, [FromBody] MovieRatingCreateDto dto)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId <= 0) return Unauthorized(new { message = "Invalid token." });

            // Block rating if user has not rented + watched/expired this movie
            var eligible = await _rentalService.IsEligibleToRateAsync(userId, id);
            if (!eligible)
                return StatusCode(403, new
                {
                    message = "You can rate this movie only after renting it."
                });

            try
            {
                var result = await _ratingService.RateMovie(id, userId, dto);
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
            if (id <= 0 || userId <= 0) return BadRequest(new { message = "Invalid ID." });

            var tokenUserId = GetUserId();
            if (tokenUserId != userId) return Forbid();

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

        // GET /api/Movie/{id}/ratings?pageNumber=1&pageSize=10
        // Returns paginated latest ratings for a movie
        [HttpGet("{id}/ratings")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRatingSummary(
            int id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var summary = await _ratingService.GetMovieRatingSummary(id);
                var paginated = await _ratingService.GetMovieRatingsPaginatedAsync(
                    id, pageNumber, pageSize);
                summary.LatestRatings = paginated;
                return Ok(summary);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Movie/{id}/rating/user/{userId}
        // Returns user's current rating + canRate eligibility in one call
        [HttpGet("{id}/rating/user/{userId}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetUserRating(int id, int userId)
        {
            if (id <= 0 || userId <= 0) return BadRequest(new { message = "Invalid ID." });
            try
            {
                var eligible = await _rentalService.IsEligibleToRateAsync(userId, id);
                var result = await _ratingService.GetUserRatingForMovie(id, userId)
                    ?? new MovieRatingResponseDto
                    {
                        MovieId = id,
                        UserId = userId,
                        RatingValue = 0,
                        RatingLabel = "Not rated",
                        IsRemoved = false
                    };
                result.CanRate = eligible;
                return Ok(result);
            }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Movie/user/{userId}/ratings
        [HttpGet("user/{userId}/ratings")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetUserRatings(int userId)
        {
            if (userId <= 0) return BadRequest(new { message = "Invalid user ID." });
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
