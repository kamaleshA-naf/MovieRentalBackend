using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models
{
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}