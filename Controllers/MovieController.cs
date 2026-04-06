using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly IWebHostEnvironment _env;

        public MovieController(IMovieService movieService, IWebHostEnvironment env)
        {
            _movieService = movieService;
            _env = env;
        }

        [Authorize(Roles = "Admin,ContentManager")]
        [HttpPost]
        public async Task<ActionResult> AddMovie(MovieCreateDto dto)
        {
            var result = await _movieService.AddMovie(dto);
            return Created($"api/Movie/{result.Id}", result);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> GetAllMovies([FromQuery] GetMoviesRequestDto request)
        {
            return Ok(await _movieService.GetMovies(request));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetMovie(int id)
        {
            return Ok(await _movieService.GetMovie(id));
        }

        [AllowAnonymous]
        [HttpGet("trending")]
        public async Task<ActionResult> GetTrendingMovies([FromQuery] int top = 10)
        {
            return Ok(await _movieService.GetTrendingMovies(top));
        }

        [Authorize(Roles = "Admin,ContentManager")]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateMovie(int id, MovieUpdateDto dto)
        {
            return Ok(await _movieService.UpdateMovie(id, dto));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMovie(int id)
        {
            return Ok(await _movieService.DeleteMovie(id));
        }

        [AllowAnonymous]
        [HttpPost("{id}/view")]
        public async Task<ActionResult> IncrementView(int id)
        {
            await _movieService.IncrementViewCountAsync(id);
            return Ok(new { message = "View counted." });
        }

        [Authorize(Roles = "Admin,ContentManager")]
        [HttpPost("upload-video")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadMovieVideo([FromForm] VideoUploadRequest request)
        {
            var videoUrl = await _movieService.UploadVideoAsync(
                request.MovieId, request.File, _env.WebRootPath ?? "wwwroot");
            return Ok(new { movieId = request.MovieId, videoUrl });
        }

        [Authorize(Roles = "Admin,ContentManager")]
        [HttpPost("upload-thumbnail")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadMovieThumbnail([FromForm] ThumbnailUploadRequest request)
        {
            var thumbnailUrl = await _movieService.UploadThumbnailAsync(
                request.MovieId, request.File, _env.WebRootPath ?? "wwwroot");
            return Ok(new { movieId = request.MovieId, thumbnailUrl });
        }
    }

    public class VideoUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public int MovieId { get; set; }
    }

    public class ThumbnailUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public int MovieId { get; set; }
    }
}
