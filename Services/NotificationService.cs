using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IRepository<int, Notification> _notificationRepository;

        public NotificationService(
            IRepository<int, Notification> notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<NotificationResponseDto> CreateNotification(
            int userId, string title, string message, string type)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepository.AddAsync(notification);
            return MapToDto(notification);
        }

        public async Task<IEnumerable<NotificationResponseDto>>
            GetNotificationsByUser(int userId)
        {
            var all = await _notificationRepository
                .FindAsync(n => n.UserId == userId);
            return all.OrderByDescending(n => n.CreatedAt)
                      .Select(MapToDto);
        }

        public async Task MarkAsRead(int notificationId)
        {
            var n = await _notificationRepository
                .GetByIdAsync(notificationId);
            if (n == null)
                throw new EntityNotFoundException(
                    "Notification", notificationId);
            n.IsRead = true;
            await _notificationRepository.UpdateAsync(notificationId, n);
        }

        public async Task DeleteNotification(int notificationId)
        {
            var n = await _notificationRepository
                .GetByIdAsync(notificationId);
            if (n == null)
                throw new EntityNotFoundException(
                    "Notification", notificationId);
            await _notificationRepository.DeleteAsync(notificationId);
        }

        private static NotificationResponseDto MapToDto(Notification n) => new()
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        };
    }
}