using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationResponseDto> CreateNotification(
            int userId, string title, string message, string type);
        Task<IEnumerable<NotificationResponseDto>> GetNotificationsByUser(
            int userId);
        Task MarkAsRead(int notificationId);
        Task DeleteNotification(
            int notificationId);
    }
}