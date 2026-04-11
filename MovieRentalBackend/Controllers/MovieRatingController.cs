using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;
using System.Security.Claims;

namespace MovieRentalApp.Controllers
{
    [Route("api/Movie")]
    [ApiController]
    [Authorize]
    public class MovieRatingController : ControllerBase
    {
        private readonly IMovieRatingService _ratingService;
        private readonly IRentalService _rentalService;

        public MovieRatingController(IMovieRatingService ratingService, IRentalService rentalService)
        {
            _ratingService = ratingService;
            _rentalService = rentalService;
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("{id}/rate")]
        public async Task<ActionResult> RateMovie(int id, MovieRatingCreateDto dto)
        {
            try
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (claim == null || !int.TryParse(claim, out var userId))
                    throw new UnauthorizedException("Invalid or missing user identity in token.");

                return Ok(await _ratingService.RateMovie(id, userId, dto));
            }
            catch { throw; }
        }

        [AllowAnonymous]
        [HttpGet("{id}/ratings")]
        public async Task<ActionResult> GetRatingSummary(int id, [FromQuery] GetMovieRatingsRequestDto request)
        {
            try
            {
                var summary = await _ratingService.GetMovieRatingSummary(id);
                summary.LatestRatings = await _ratingService.GetMovieRatingsPaginatedAsync(id, request);
                return Ok(summary);
            }
            catch { throw; }
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("{id}/rating/user/{userId}")]
        public async Task<ActionResult> GetUserRating(int id, int userId)
        {
            try
            {
                var eligible = await _rentalService.IsEligibleToRateAsync(userId, id);
                var result = await _ratingService.GetUserRatingForMovie(id, userId)
                    ?? new MovieRatingResponseDto { MovieId = id, UserId = userId, RatingValue = 0, RatingLabel = "Not rated" };
                result.CanRate = eligible;
                return Ok(result);
            }
            catch { throw; }
        }
    }
}
