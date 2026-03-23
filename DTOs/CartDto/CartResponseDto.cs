namespace MovieRentalApp.Models.DTOs
{
    public class CartResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public decimal RentalPrice { get; set; }
        public int DurationDays { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime AddedAt { get; set; }
        public List<GenreResponseDto> Genres { get; set; } = new();
    }
}