
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

        // ── Helpers ───────────────────────────────────────────────
        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id : 0;

        private string GetUserName() =>
            User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        private string GetRole() =>
            User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

        // ── ADD MOVIE ─────────────────────────────────────────────
        [HttpPost("add")]
        [Authorize(Roles = "Admin,ContentManager")]
        public async Task<IActionResult> AddMovie([FromBody] MovieCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _movieService.AddMovie(dto);

                await _auditLog.LogAsync(
                    GetUserId(), GetUserName(), GetRole(),
                    $"Added new movie '{dto.Title}' (ID: {result.Id}).", "");

                return CreatedAtAction(nameof(GetMovie), new { id = result.Id }, result);
            }
            catch (BusinessRuleViolationException ex)
            { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        // ── UPLOAD VIDEO ──────────────────────────────────────────
        [HttpPost("upload-video")]
        [Authorize(Roles = "Admin,ContentManager")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMovieVideo(
            [FromForm] VideoUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var allowed = new[] { "video/mp4", "video/avi", "video/x-matroska", "video/webm" };
            if (!allowed.Contains(request.File.ContentType.ToLower()))
                return BadRequest(new { message = "Invalid file type. Allowed: mp4, avi, mkv, webm." });

            if (request.File.Length > 500L * 1024 * 1024)
                return BadRequest(new { message = "File too large. Maximum 500MB." });

            if (request.MovieId <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

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
                    Title = movie.Title,
                    Description = movie.Description,
                    RentalPrice = movie.RentalPrice,
                    Director = movie.Director,
                    ReleaseYear = movie.ReleaseYear,
                    Rating = movie.Rating,
                    VideoUrl = videoUrl
                });

                await _auditLog.LogAsync(
                    GetUserId(), GetUserName(), GetRole(),
                    $"Uploaded video for movie '{movie.Title}' (ID: {request.MovieId}).", "");

                return Ok(new { message = "Video uploaded successfully.", movieId = request.MovieId, videoUrl });
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ── UPLOAD THUMBNAIL ──────────────────────────────────────
        [HttpPost("upload-thumbnail")]
        [Authorize(Roles = "Admin,ContentManager")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMovieThumbnail(
            [FromForm] ThumbnailUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var allowed = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowed.Contains(request.File.ContentType.ToLower()))
                return BadRequest(new { message = "Invalid file type. Allowed: JPEG, PNG, WebP." });

            if (request.File.Length > 5L * 1024 * 1024)
                return BadRequest(new { message = "File too large. Maximum 5MB." });

            if (request.MovieId <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

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
                    Title = movie.Title,
                    Description = movie.Description,
                    RentalPrice = movie.RentalPrice,
                    Director = movie.Director,
                    ReleaseYear = movie.ReleaseYear,
                    Rating = movie.Rating,
                    ThumbnailUrl = thumbnailUrl
                });

                return Ok(new { message = "Thumbnail uploaded.", movieId = request.MovieId, thumbnailUrl });
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ── STREAM VIDEO ──────────────────────────────────────────
        // Serves the video file from disk with proper 206 range support.
        // Frontend: <video src="/api/movie/5/stream" controls />
        [HttpGet("{id}/stream")]
        [Authorize]
        public async Task<IActionResult> StreamVideo(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var movie = await _movieService.GetMovie(id);

                if (string.IsNullOrEmpty(movie.VideoUrl))
                    return NotFound(new { message = "No video available for this movie." });

                // VideoUrl is stored as "/uploads/movies/filename.mp4"
                var relativePath = movie.VideoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", relativePath);

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { message = "Video file not found on server." });

                // PhysicalFile with EnableRangeProcessing=true handles all
                // Range header parsing, 206 responses, and Content-Range headers.
                return PhysicalFile(filePath, "video/mp4", enableRangeProcessing: true);
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ── WATCH (increment ViewCount + return movie) ────────────
        // POST /api/movie/{id}/watch
        // Frontend calls this on "Watch Now" click
        [HttpPost("{id}/watch")]
        [AllowAnonymous]
        public async Task<IActionResult> WatchMovie(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var incremented = await _movieService.IncrementViewCountAsync(id);
                if (!incremented)
                    return NotFound(new { message = "Movie not found or is inactive." });

                // Return updated movie so frontend has fresh ViewCount
                var movie = await _movieService.GetMovie(id);
                return Ok(movie);
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ── INCREMENT VIEW (kept for backward compat) ─────────────
        // POST /api/movie/{id}/view
        [HttpPost("{id}/view")]
        [AllowAnonymous]
        public async Task<IActionResult> IncrementView(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });

            var incremented = await _movieService.IncrementViewCountAsync(id);
            if (!incremented)
                return NotFound(new { message = "Movie not found or is inactive." });

            return Ok(new { message = "View counted." });
        }

        // ── GET MOVIE ─────────────────────────────────────────────
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

        // ── GET ALL ───────────────────────────────────────────────
        // Supports infinite scroll + filters
        // GET /api/Movie?pageNumber=1&pageSize=20&genreId=1&language=Tamil&minRating=3&sortBy=Title&sortDirection=asc
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllMovies(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? genreId = null,
            [FromQuery] string? language = null,
            [FromQuery] double? minRating = null,
            [FromQuery] string sortBy = "Id",
            [FromQuery] string sortDirection = "desc")
        {
            var pagination = new PaginationDto { PageNumber = pageNumber, PageSize = pageSize };
            try
            {
                var result = await _movieService.GetAllMovies(
                    pagination, genreId, language, minRating, sortBy, sortDirection);
                return Ok(result);
            }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        // ── SEARCH ────────────────────────────────────────────────
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchMovies(
            [FromQuery] string? keyword,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new { message = "Keyword cannot be empty." });

            var pagination = new PaginationDto { PageNumber = pageNumber, PageSize = pageSize };
            try
            {
                var result = await _movieService.SearchMovies(keyword, pagination);
                return Ok(result);
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ── BY GENRE ──────────────────────────────────────────────
        [HttpGet("genre/{genreId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMoviesByGenre(
            int genreId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (genreId <= 0) return BadRequest(new { message = "Invalid genre ID." });

            var pagination = new PaginationDto { PageNumber = pageNumber, PageSize = pageSize };
            try
            {
                var result = await _movieService.GetMoviesByGenre(genreId, pagination);
                return Ok(result);
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ── TRENDING ──────────────────────────────────────────────
        // GET /api/movie/trending?top=10
        // Returns top movies ordered by ViewCount DESC
        [HttpGet("trending")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTrendingMovies([FromQuery] int top = 10)
        {
            try
            {
                // Always use ViewCount — most watched = trending
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

        // ── MOVIE STATS ───────────────────────────────────────────
        [HttpGet("{id}/stats")]
        [Authorize(Roles = "Admin,ContentManager")]
        public async Task<IActionResult> GetMovieStats(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var rentalCount = await _context.Rentals.CountAsync(r => r.MovieId == id);
                var activeRentals = await _context.Rentals
                    .CountAsync(r => r.MovieId == id && r.Status == "Active");
                var totalRevenue = await _context.Payments
                    .Where(p => p.MovieId == id && p.Status == "Completed")
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                return Ok(new { movieId = id, rentalCount, activeRentals, totalRevenue });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ── UPDATE MOVIE ──────────────────────────────────────────
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ContentManager")]
        public async Task<IActionResult> UpdateMovie(
            int id, [FromBody] MovieUpdateDto dto)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                // Detect what changed for the audit log message
                var existingMovie = await _movieService.GetMovie(id);
                var result = await _movieService.UpdateMovie(id, dto);

                // ✅ Build descriptive audit log
                string actionDesc;
                if (dto.IsActive.HasValue)
                {
                    actionDesc = dto.IsActive.Value
                        ? $"Activated movie '{existingMovie.Title}' (ID: {id}) — now visible to customers."
                        : $"Paused movie '{existingMovie.Title}' (ID: {id}) — hidden from customers.";
                }
                else
                {
                    actionDesc = $"Updated movie '{existingMovie.Title}' (ID: {id}) details.";
                }

                await _auditLog.LogAsync(
                    GetUserId(), GetUserName(), GetRole(), actionDesc, "");

                return Ok(result);
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ── SOFT DELETE ───────────────────────────────────────────
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var existingMovie = await _movieService.GetMovie(id);
                var result = await _movieService.DeleteMovie(id);

                // ✅ Audit log for soft delete
                await _auditLog.LogAsync(
                    GetUserId(), GetUserName(), GetRole(),
                    $"Soft-deleted movie '{existingMovie.Title}' (ID: {id}). " +
                    $"Movie is now hidden from all customers.", "");

                return Ok(new { message = "Movie deleted.", data = result });
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }

    // ── DTO classes for file upload ───────────────────────────────
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