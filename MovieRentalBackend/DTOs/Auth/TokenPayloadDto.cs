namespace MovieRentalApp.Models.DTOs
{
    public class TokenPayloadDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}