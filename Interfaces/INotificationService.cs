using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface INotificationService
    {
        // TODO: remove or implement — CreateNotification is never called by any controller or service.
        // No frontend endpoint triggers this. Either wire it up (e.g. after rental/payment) or remove it.
        Task<NotificationResponseDto> CreateNotification(
            int userId, string title, string message, string type);
        Task<IEnumerable<NotificationResponseDto>> GetNotificationsByUser(
            int userId);
        Task MarkAsRead(int notificationId);
        Task DeleteNotification(
            int notificationId);
    }
}