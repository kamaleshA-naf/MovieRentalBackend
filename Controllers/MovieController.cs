using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieRentalApp.Contexts;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;
using MovieRentalApp.Services;
using System.Security.Claims;

namespace MovieRentalApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly IWebHostEnvironment _env;
        private readonly MovieContext _context;
        private readonly AuditLogService _auditLog;

        public MovieController(
            IMovieService movieService,
            IWebHostEnvironment env,
            MovieContext context,
            AuditLogService auditLog)
        {
            _movieService = movieService;
            _env = env;
            _context = context;
            _auditLog = auditLog;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
        private string GetUserName() => User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        private string GetRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

        // POST /api/Movie/add
        [HttpPost("add")]
        [Authorize(Roles = "Admin,ContentManager")]
        public async Task<IActionResult> AddMovie([FromBody] MovieCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _movieService.AddMovie(dto);
                await _auditLog.LogAsync(GetUserId(), GetUserName(), GetRole(),
                    $"Added new movie '{dto.Title}' (ID: {result.Id}).", "");
                return CreatedAtAction(nameof(GetMovie), new { id = result.Id }, result);
            }
            catch (BusinessRuleViolationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST /api/Movie/upload-video
        [HttpPost("upload-video")]
        [Authorize(Roles = "Admin,ContentManager")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMovieVideo([FromForm] VideoUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { message = "No file uploaded." });
            var allowed = new[] { "video/mp4", "video/avi", "video/x-matroska", "video/webm" };
            if (!allowed.Contains(request.File.ContentType.ToLower()))
                return BadRequest(new { message = "Invalid file type." });
            if (request.File.Length > 500L * 1024 * 1024)
                return BadRequest(new { message = "File too large. Maximum 500MB." });
            if (request.MovieId <= 0) return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var movie = await _movieService.GetMovie(request.MovieId);
                var folder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "movies");
                Directory.CreateDirectory(folder);
                var ext = Path.GetExtension(request.File.FileName).ToLower();
                var fileName = $"movie_{request.MovieId}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await request.File.CopyToAsync(stream);
                var videoUrl = $"/uploads/movies/{fileName}";
                await _movieService.UpdateMovie(request.MovieId, new MovieUpdateDto
                {
                    Title = movie.Title, Description = movie.Description,
                    RentalPrice = movie.RentalPrice, Director = movie.Director,
                    ReleaseYear = movie.ReleaseYear, Rating = movie.Rating, VideoUrl = videoUrl
                });
                await _auditLog.LogAsync(GetUserId(), GetUserName(), GetRole(),
                    $"Uploaded video for movie '{movie.Title}' (ID: {request.MovieId}).", "");
                return Ok(new { message = "Video uploaded successfully.", movieId = request.MovieId, videoUrl });
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST /api/Movie/upload-thumbnail
        [HttpPost("upload-thumbnail")]
        [Authorize(Roles = "Admin,ContentManager")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMovieThumbnail([FromForm] ThumbnailUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { message = "No file uploaded." });
            var allowed = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowed.Contains(request.File.ContentType.ToLower()))
                return BadRequest(new { message = "Invalid file type." });
            if (request.File.Length > 5L * 1024 * 1024)
                return BadRequest(new { message = "File too large. Maximum 5MB." });
            if (request.MovieId <= 0) return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var movie = await _movieService.GetMovie(request.MovieId);
                var folder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "thumbnails");
                Directory.CreateDirectory(folder);
                var ext = Path.GetExtension(request.File.FileName).ToLower();
                var fileName = $"thumb_{request.MovieId}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await request.File.CopyToAsync(stream);
                var thumbnailUrl = $"/uploads/thumbnails/{fileName}";
                await _movieService.UpdateMovie(request.MovieId, new MovieUpdateDto
                {
                    Title = movie.Title, Description = movie.Description,
                    RentalPrice = movie.RentalPrice, Director = movie.Director,
                    ReleaseYear = movie.ReleaseYear, Rating = movie.Rating, ThumbnailUrl = thumbnailUrl
                });
                return Ok(new { message = "Thumbnail uploaded.", movieId = request.MovieId, thumbnailUrl });
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST /api/Movie/{id}/view  — increment ViewCount
        [HttpPost("{id}/view")]
        [AllowAnonymous]
        public async Task<IActionResult> IncrementView(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });
            var incremented = await _movieService.IncrementViewCountAsync(id);
            if (!incremented) return NotFound(new { message = "Movie not found or is inactive." });
            return Ok(new { message = "View counted." });
        }

        // GET /api/Movie/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMovie(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var result = await _movieService.GetMovie(id);
                return Ok(result);
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Movie?pageNumber=&pageSize=&genreId=&language=&minRating=&sortBy=&sortDirection=
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllMovies(
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
            [FromQuery] int? genreId = null, [FromQuery] string? language = null,
            [FromQuery] double? minRating = null,
            [FromQuery] string sortBy = "Id", [FromQuery] string sortDirection = "desc")
        {
            var pagination = new PaginationDto { PageNumber = pageNumber, PageSize = pageSize };
            try
            {
                var result = await _movieService.GetAllMovies(
                    pagination, genreId, language, minRating, sortBy, sortDirection);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Movie/trending
        [HttpGet("trending")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTrendingMovies([FromQuery] int top = 10)
        {
            try
            {
                var trendingIds = await _context.Movies
                    .Where(m => m.IsActive)
                    .OrderByDescending(m => m.ViewCount)
                    .ThenByDescending(m => m.CreatedAt)
                    .Take(top)
                    .Select(m => m.Id)
                    .ToListAsync();
                var result = await _movieService.GetTrendingMovies(trendingIds);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // PUT /api/Movie/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ContentManager")]
        public async Task<IActionResult> UpdateMovie(int id, [FromBody] MovieUpdateDto dto)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var existingMovie = await _movieService.GetMovie(id);
                var result = await _movieService.UpdateMovie(id, dto);
                var actionDesc = dto.IsActive.HasValue
                    ? (dto.IsActive.Value
                        ? $"Activated movie '{existingMovie.Title}' (ID: {id})."
                        : $"Paused movie '{existingMovie.Title}' (ID: {id}).")
                    : $"Updated movie '{existingMovie.Title}' (ID: {id}) details.";
                await _auditLog.LogAsync(GetUserId(), GetUserName(), GetRole(), actionDesc, "");
                return Ok(result);
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // DELETE /api/Movie/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var existingMovie = await _movieService.GetMovie(id);
                var result = await _movieService.DeleteMovie(id);
                await _auditLog.LogAsync(GetUserId(), GetUserName(), GetRole(),
                    $"Soft-deleted movie '{existingMovie.Title}' (ID: {id}).", "");
                return Ok(new { message = "Movie deleted.", data = result });
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
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
