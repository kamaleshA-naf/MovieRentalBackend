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

        [Authorize(Roles = "Admin")]
        [HttpPost("add")]
        public async Task<ActionResult> AddMovie(MovieCreateDto dto)
        {
            try
            {
                var result = await _movieService.AddMovie(dto);
                return Created($"api/Movie/{result.Id}", result);
            }
            catch { throw; }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> GetAllMovies([FromQuery] GetMoviesRequestDto request)
        {
            try
            {
                return Ok(await _movieService.GetMovies(request));
            }
            catch { throw; }
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetMovie(int id)
        {
            try
            {
                return Ok(await _movieService.GetMovie(id));
            }
            catch { throw; }
        }

        [AllowAnonymous]
        [HttpGet("trending")]
        public async Task<ActionResult> GetTrendingMovies([FromQuery] int top = 10)
        {
            try
            {
                return Ok(await _movieService.GetTrendingMovies(top));
            }
            catch { throw; }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateMovie(int id, MovieUpdateDto dto)
        {
            try
            {
                return Ok(await _movieService.UpdateMovie(id, dto));
            }
            catch { throw; }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMovie(int id)
        {
            try
            {
                return Ok(await _movieService.DeleteMovie(id));
            }
            catch { throw; }
        }

        [AllowAnonymous]
        [HttpPost("{id}/view")]
        public async Task<ActionResult> IncrementView(int id)
        {
            try
            {
                await _movieService.IncrementViewCountAsync(id);
                return Ok(new { message = "View counted." });
            }
            catch { throw; }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("upload-video")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadMovieVideo([FromForm] VideoUploadRequest request)
        {
            try
            {
                var videoUrl = await _movieService.UploadVideoAsync(
                    request.MovieId, request.File, _env.WebRootPath ?? "wwwroot");
                return Ok(new { movieId = request.MovieId, videoUrl });
            }
            catch { throw; }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("upload-thumbnail")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadMovieThumbnail([FromForm] ThumbnailUploadRequest request)
        {
            try
            {
                var thumbnailUrl = await _movieService.UploadThumbnailAsync(
                    request.MovieId, request.File, _env.WebRootPath ?? "wwwroot");
                return Ok(new { movieId = request.MovieId, thumbnailUrl });
            }
            catch { throw; }
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
