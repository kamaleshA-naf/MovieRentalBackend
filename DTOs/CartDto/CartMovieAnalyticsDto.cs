namespace MovieRentalApp.Models.DTOs
{
    public class CartMovieAnalyticsDto
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int CartCount { get; set; }
        public int RentalCount { get; set; }
        public decimal ConversionRate { get; set; }
    }
}