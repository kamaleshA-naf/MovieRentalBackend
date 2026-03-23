using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieRentalApp.Contexts;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly IWebHostEnvironment _env;
        private readonly MovieContext _context;

        public MovieController(
            IMovieService movieService,
            IWebHostEnvironment env,
            MovieContext context)
        {
            _movieService = movieService;
            _env = env;
            _context = context;
        }

        [HttpPost("AddMovie")]
        [Authorize(Roles = "Admin,ContentManager")]
        public async Task<IActionResult> AddMovie(
            [FromBody] MovieCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _movieService.AddMovie(dto);
                return CreatedAtAction(
                    nameof(GetMovie), new { id = result.Id }, result);
            }
            catch (BusinessRuleViolationException ex)
            { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("upload-video")]
        [Authorize(Roles = "Admin,ContentManager")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMovieVideo(
            [FromForm] VideoUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var allowed = new[]
            {
                "video/mp4", "video/avi",
                "video/x-matroska", "video/webm"
            };
            if (!allowed.Contains(
                    request.File.ContentType.ToLower()))
                return BadRequest(new
                {
                    message = "Invalid file type. " +
                              "Allowed: mp4, avi, mkv, webm."
                });

            if (request.File.Length > 500L * 1024 * 1024)
                return BadRequest(new
                {
                    message = "File too large. Maximum 500MB."
                });

            if (request.MovieId <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

            try
            {
                var movie = await _movieService.GetMovie(request.MovieId);

                var folder = Path.Combine(
                    _env.WebRootPath ?? "wwwroot",
                    "uploads", "movies");
                Directory.CreateDirectory(folder);

                var ext = Path.GetExtension(
                    request.File.FileName).ToLower();
                var fileName = $"movie_{request.MovieId}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(
                    filePath, FileMode.Create))
                    await request.File.CopyToAsync(stream);

                var videoUrl = $"/uploads/movies/{fileName}";

                await _movieService.UpdateMovie(
                    request.MovieId,
                    new MovieUpdateDto
                    {
                        Title = movie.Title,
                        Description = movie.Description,
                        RentalPrice = movie.RentalPrice,
                        Director = movie.Director,
                        ReleaseYear = movie.ReleaseYear,
                        Rating = movie.Rating,
                        VideoUrl = videoUrl
                    });

                return Ok(new
                {
                    message = "Video uploaded successfully.",
                    movieId = request.MovieId,
                    videoUrl
                });
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("upload-thumbnail")]
        [Authorize(Roles = "Admin,ContentManager")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMovieThumbnail(
            [FromForm] ThumbnailUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var allowed = new[]
            {
                "image/jpeg", "image/jpg",
                "image/png",  "image/webp"
            };
            if (!allowed.Contains(
                    request.File.ContentType.ToLower()))
                return BadRequest(new
                {
                    message = "Invalid file type. Allowed: JPEG, PNG, WebP."
                });

            if (request.File.Length > 5L * 1024 * 1024)
                return BadRequest(new
                {
                    message = "File too large. Maximum 5MB."
                });

            if (request.MovieId <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

            try
            {
                var movie = await _movieService.GetMovie(request.MovieId);

                var folder = Path.Combine(
                    _env.WebRootPath ?? "wwwroot",
                    "uploads", "thumbnails");
                Directory.CreateDirectory(folder);

                var ext = Path.GetExtension(
                    request.File.FileName).ToLower();
                var fileName =
                    $"thumb_{request.MovieId}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(
                    filePath, FileMode.Create))
                    await request.File.CopyToAsync(stream);

                var thumbnailUrl = $"/uploads/thumbnails/{fileName}";

                await _movieService.UpdateMovie(
                    request.MovieId,
                    new MovieUpdateDto
                    {
                        Title = movie.Title,
                        Description = movie.Description,
                        RentalPrice = movie.RentalPrice,
                        Director = movie.Director,
                        ReleaseYear = movie.ReleaseYear,
                        Rating = movie.Rating,
                        ThumbnailUrl = thumbnailUrl
                    });

                return Ok(new
                {
                    message = "Thumbnail uploaded.",
                    movieId = request.MovieId,
                    thumbnailUrl
                });
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("{id}/view")]
        [AllowAnonymous]
        public async Task<IActionResult> IncrementView(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                return Ok(new { message = "View counted." });
            }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMovie(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var result = await _movieService.GetMovie(id);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllMovies(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var pagination = new PaginationDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            try
            {
                var result = await _movieService.GetAllMovies(pagination);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchMovies(
            [FromQuery] string? keyword,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new { message = "Keyword cannot be empty." });

            var pagination = new PaginationDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            try
            {
                var result = await _movieService
                    .SearchMovies(keyword, pagination);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex)
            { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("genre/{genreId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMoviesByGenre(
            int genreId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (genreId <= 0)
                return BadRequest(new { message = "Invalid genre ID." });

            var pagination = new PaginationDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            try
            {
                var result = await _movieService
                    .GetMoviesByGenre(genreId, pagination);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex)
            { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("trending")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTrendingMovies(
            [FromQuery] int top = 5)
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-7);

                var trendingIds = await _context.Rentals
                    .Where(r => r.RentalDate >= cutoff)
                    .GroupBy(r => r.MovieId)
                    .Select(g => new { MovieId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(top)
                    .Select(x => x.MovieId)
                    .ToListAsync();

                if (!trendingIds.Any())
                {
                    trendingIds = await _context.Movies
                        .Where(m => m.IsActive)
                        .OrderByDescending(m => m.CreatedAt)
                        .Take(top)
                        .Select(m => m.Id)
                        .ToListAsync();
                }

                var result = await _movieService
                    .GetTrendingMovies(trendingIds);
                return Ok(result);
            }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("{id}/stats")]
        [Authorize(Roles = "Admin,ContentManager")]
        public async Task<IActionResult> GetMovieStats(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var rentalCount = await _context.Rentals
                    .CountAsync(r => r.MovieId == id);
                var activeRentals = await _context.Rentals
                    .CountAsync(r => r.MovieId == id &&
                                     r.Status == "Active");
                var totalRevenue = await _context.Payments
                    .Where(p => p.MovieId == id &&
                                p.Status == "Completed")
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                return Ok(new
                {
                    movieId = id,
                    rentalCount,
                    activeRentals,
                    totalRevenue
                });
            }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ContentManager")]
        public async Task<IActionResult> UpdateMovie(
            int id, [FromBody] MovieUpdateDto dto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var result = await _movieService.UpdateMovie(id, dto);
                return Ok(result);
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex)
            { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid movie ID." });
            try
            {
                var result = await _movieService.DeleteMovie(id);
                return Ok(new { message = "Movie deleted.", data = result });
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
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