namespace MovieRentalApp.Models.DTOs
{
    public class AuditLogResponseDto
    {
        public int Id { get; set; }       // frontend expects "id"
        public int LogId { get; set; }    // kept for backward compat
        public string Message { get; set; } = string.Empty;
        public string ErrorNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
