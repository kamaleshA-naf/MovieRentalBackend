using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Interfaces;

namespace MovieRentalApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer,Admin")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetByUser(int userId)
        {
            return Ok(await _notificationService.GetNotificationsByUser(userId));
        }

        [HttpPut("{id}/read")]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsRead(id);
            return Ok(new { message = "Marked as read." });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _notificationService.DeleteNotification(id);
            return Ok(new { message = "Notification deleted." });
        }
    }
}
