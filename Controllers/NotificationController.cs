using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;

namespace MovieRentalApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(
            INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID." });
            try
            {
                var result = await _notificationService
                    .GetNotificationsByUser(userId);
                return Ok(result);
            }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid ID." });
            try
            {
                await _notificationService.MarkAsRead(id);
                return Ok(new { message = "Marked as read." });
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid ID." });
            try
            {
                await _notificationService.DeleteNotification(id);
                return Ok(new { message = "Notification deleted." });
            }
            catch (EntityNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}