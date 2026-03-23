namespace MovieRentalApp.Models.DTOs
{
    public class AuditLogResponseDto
    {
        public int LogId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}