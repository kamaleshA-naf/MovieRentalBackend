namespace MovieRentalApp.Models.DTOs
{
    public class CheckoutItemDto
    {
        public int MovieId { get; set; }
        public int DurationDays { get; set; }
        public decimal TotalCost { get; set; }
    }
}
