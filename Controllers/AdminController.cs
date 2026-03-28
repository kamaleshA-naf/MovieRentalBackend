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

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try { return Ok(await _adminService.GetDashboardStats()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/users?pageNumber=1&pageSize=20&sortBy=UserName&sortDirection=asc
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "UserName",
            [FromQuery] string sortDirection = "asc")
        {
            try
            {
                var result = await _adminService.GetUsersPaginatedAsync(
                    pageNumber, pageSize, sortBy, sortDirection);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("users/today")]
        public async Task<IActionResult> GetUsersToday()
        {
            try { return Ok(await _adminService.GetUsersTodayAsync()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("users/rentals")]
        public async Task<IActionResult> GetAllUsersWithRentals()
        {
            try { return Ok(await _adminService.GetAllUsersWithRentals()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/payments?pageNumber=1&pageSize=20&sortBy=PaymentDate&sortDirection=desc
        [HttpGet("payments")]
        public async Task<IActionResult> GetAllPayments(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "PaymentDate",
            [FromQuery] string sortDirection = "desc")
        {
            try
            {
                var result = await _adminService.GetPaymentsPaginatedAsync(
                    pageNumber, pageSize, sortBy, sortDirection);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

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

        // GET /api/Admin/rentals?pageNumber=1&pageSize=20&sortBy=RentalDate&sortDirection=desc
        [HttpGet("rentals")]
        public async Task<IActionResult> GetAllRentals(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "RentalDate",
            [FromQuery] string sortDirection = "desc")
        {
            try
            {
                var result = await _adminService.GetRentalsPaginatedAsync(
                    pageNumber, pageSize, sortBy, sortDirection);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/logs?pageNumber=1&pageSize=20
        [HttpGet("logs")]
        public async Task<IActionResult> GetAllLogs(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _adminService.GetLogsPaginatedAsync(pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("logs/user/{userId}")]
        public async Task<IActionResult> GetLogsByUser(int userId)
        {
            if (userId <= 0) return BadRequest(new { message = "Invalid user ID." });
            try { return Ok(await _adminService.GetLogsByUser(userId)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueSummary()
        {
            try { return Ok(await _adminService.GetRevenueSummary()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
