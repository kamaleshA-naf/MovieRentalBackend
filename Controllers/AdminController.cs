using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult> GetDashboardStats()
        {
            return Ok(await _adminService.GetDashboardStats());
        }

        [HttpGet("users/rentals")]
        public async Task<ActionResult> GetAllUsersWithRentals()
        {
            return Ok(await _adminService.GetAllUsersWithRentals());
        }

        [HttpGet("revenue")]
        public async Task<ActionResult> GetRevenueSummary()
        {
            return Ok(await _adminService.GetRevenueSummary());
        }

        [HttpGet("payments")]
        public async Task<ActionResult> GetAllPayments([FromQuery] GetPaymentsRequestDto request)
        {
            return Ok(await _adminService.GetPayments(request));
        }

        [HttpGet("logs")]
        public async Task<ActionResult> GetAllLogs([FromQuery] GetLogsRequestDto request)
        {
            return Ok(await _adminService.GetLogs(request));
        }
    }
}
