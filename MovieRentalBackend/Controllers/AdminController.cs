using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase        //A base class for API controllers
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;                           //ActionResult - Represents the HTTP response returned by your API.
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult> GetDashboardStats()
        {
            try
            {
                return Ok(await _adminService.GetDashboardStats());
            }
            catch { throw; }
        }

        [HttpGet("users/rentals")]
        public async Task<ActionResult> GetAllUsersWithRentals()
        {
            try
            {
                return Ok(await _adminService.GetAllUsersWithRentals());
            }
            catch { throw; }              ///It also gives flexibility to add logging or custom handling later if required.”
        }

        [HttpGet("revenue")]
        public async Task<ActionResult> GetRevenueSummary()
        {
            try
            {
                return Ok(await _adminService.GetRevenueSummary());
            }
            catch { throw; }
        }

        [HttpGet("payments")]
        public async Task<ActionResult> GetAllPayments([FromQuery] GetPaymentsRequestDto request)
        {
            try
            {
                return Ok(await _adminService.GetPayments(request));
            }
            catch { throw; }
        }

        [HttpGet("logs")]
        public async Task<ActionResult> GetAllLogs([FromQuery] GetLogsRequestDto request)
        {
            try
            {
                return Ok(await _adminService.GetLogs(request));
            }
            catch { throw; }
        }
    }
}
