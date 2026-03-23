namespace MovieRentalApp.Models.DTOs
{
    public class WishlistResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public decimal RentalPrice { get; set; }
        public DateTime AddedDate { get; set; }
        public string? ThumbnailUrl { get; set; }

    }
}