namespace MovieRentalApp.Models.DTOs
{
    public class UserRentalSummaryDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalRentals { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime DateJoined => CreatedAt;  // alias — frontend expects dateJoined
        public List<RentalResponseDto> Rentals { get; set; } = new();
    }
}
