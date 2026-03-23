namespace MovieRentalApp.Models.DTOs
{
    public class UserRentalSummaryDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalRentals { get; set; }
        public List<RentalResponseDto> Rentals { get; set; } = new();
    }
}