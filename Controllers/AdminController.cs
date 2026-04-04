using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // GET /api/Admin/users/rentals
        [HttpGet("users/rentals")]
        public async Task<IActionResult> GetAllUsersWithRentals()
        {
            try { return Ok(await _adminService.GetAllUsersWithRentals()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/payments?pageNumber=1&pageSize=20&sortBy=paymentdate&status=Failed&method=UPI
        [HttpGet("payments")]
        public async Task<IActionResult> GetAllPayments(
            [FromQuery] int    pageNumber    = 1,
            [FromQuery] int    pageSize      = 20,
            [FromQuery] string sortBy        = "paymentdate",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] string? status       = null,
            [FromQuery] string? method       = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;
            try
            {
                var result = await _adminService.GetPaymentsPaginatedAsync(
                    pageNumber, pageSize, sortBy, sortDirection, status, method);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/Admin/logs?pageNumber=1&pageSize=20&sortBy=createdat&search=admin
        [HttpGet("logs")]
        public async Task<IActionResult> GetAllLogs(
            [FromQuery] int    pageNumber    = 1,
            [FromQuery] int    pageSize      = 20,
            [FromQuery] string sortBy        = "createdat",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] string? search       = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;
            try
            {
                var result = await _adminService.GetLogsPaginatedAsync(
                    pageNumber, pageSize, sortBy, sortDirection, search);
                return Ok(result);
            }
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
