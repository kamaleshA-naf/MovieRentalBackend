namespace MovieRentalApp.Models.DTOs
{
    public class CartAnalyticsDto
    {
        public int TotalCartItems { get; set; }
        public int TotalUniqueMoviesInCarts { get; set; }
        public decimal ConversionRate { get; set; }
        public List<CartMovieAnalyticsDto> TopAbandonedMovies { get; set; } = new();
        public List<CartMovieAnalyticsDto> TopCartedMovies { get; set; } = new();
    }
}