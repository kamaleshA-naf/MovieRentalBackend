using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;

namespace MovieRentalApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // GET /api/Admin/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try { return Ok(await _adminService.GetDashboardStats()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/users?pageNumber=1&pageSize=20&sortBy=name&sortDirection=asc&search=john&role=Customer
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int    pageNumber     = 1,
            [FromQuery] int    pageSize       = 20,
            [FromQuery] string sortBy         = "name",
            [FromQuery] string sortDirection  = "asc",
            [FromQuery] string? search        = null,
            [FromQuery] string? role          = null)
        {
            try
            {
                var result = await _adminService.GetUsersPaginatedAsync(
                    pageNumber, pageSize, sortBy, sortDirection, search, role);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/users/rentals
        [HttpGet("users/rentals")]
        public async Task<IActionResult> GetAllUsersWithRentals()
        {
            try { return Ok(await _adminService.GetAllUsersWithRentals()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/movies?pageNumber=1&pageSize=20&sortBy=title&search=avengers&genreId=1&language=English&isActive=true
        [HttpGet("movies")]
        public async Task<IActionResult> GetMovies(
            [FromQuery] int    pageNumber    = 1,
            [FromQuery] int    pageSize      = 20,
            [FromQuery] string sortBy        = "id",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] string? search       = null,
            [FromQuery] int?   genreId       = null,
            [FromQuery] string? language     = null,
            [FromQuery] bool?  isActive      = null)
        {
            try
            {
                var result = await _adminService.GetMoviesPaginatedAsync(
                    pageNumber, pageSize, sortBy, sortDirection,
                    search, genreId, language, isActive);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/payments?pageNumber=1&pageSize=20&sortBy=paymentdate&sortDirection=desc&status=Failed&method=UPI
        [HttpGet("payments")]
        public async Task<IActionResult> GetAllPayments(
            [FromQuery] int    pageNumber    = 1,
            [FromQuery] int    pageSize      = 20,
            [FromQuery] string sortBy        = "paymentdate",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] string? status       = null,
            [FromQuery] string? method       = null)
        {
            try
            {
                var result = await _adminService.GetPaymentsPaginatedAsync(
                    pageNumber, pageSize, sortBy, sortDirection, status, method);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/payments/user/{userId}
        [HttpGet("payments/user/{userId}")]
        public async Task<IActionResult> GetPaymentsByUser(int userId)
        {
            if (userId <= 0) return BadRequest(new { message = "Invalid user ID." });
            try
            {
                var result = await _adminService.GetPaymentsByUser(userId);
                return Ok(result);
            }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/rentals?pageNumber=1&pageSize=20&sortBy=rentaldate&sortDirection=desc&status=Active
        [HttpGet("rentals")]
        public async Task<IActionResult> GetAllRentals(
            [FromQuery] int    pageNumber    = 1,
            [FromQuery] int    pageSize      = 20,
            [FromQuery] string sortBy        = "rentaldate",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] string? status       = null)
        {
            try
            {
                var result = await _adminService.GetRentalsPaginatedAsync(
                    pageNumber, pageSize, sortBy, sortDirection, status);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/logs?pageNumber=1&pageSize=20&sortBy=createdat&sortDirection=desc&search=admin
        [HttpGet("logs")]
        public async Task<IActionResult> GetAllLogs(
            [FromQuery] int    pageNumber    = 1,
            [FromQuery] int    pageSize      = 20,
            [FromQuery] string sortBy        = "createdat",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] string? search       = null)
        {
            try
            {
                var result = await _adminService.GetLogsPaginatedAsync(
                    pageNumber, pageSize, sortBy, sortDirection, search);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/logs/user/{userId}
        [HttpGet("logs/user/{userId}")]
        public async Task<IActionResult> GetLogsByUser(int userId)
        {
            if (userId <= 0) return BadRequest(new { message = "Invalid user ID." });
            try { return Ok(await _adminService.GetLogsByUser(userId)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/revenue
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueSummary()
        {
            try { return Ok(await _adminService.GetRevenueSummary()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
