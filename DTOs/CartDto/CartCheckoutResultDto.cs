namespace MovieRentalApp.Models.DTOs
{
    public class CartCheckoutResultDto
    {
        public int TotalMovies { get; set; }
        public decimal TotalAmount { get; set; }
        public List<string> SkippedMovies { get; set; } = new();
        public List<RentalResponseDto> Rentals { get; set; } = new();
    }
}