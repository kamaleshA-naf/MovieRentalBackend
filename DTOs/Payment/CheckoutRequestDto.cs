namespace MovieRentalApp.Models.DTOs
{
    public class CheckoutRequestDto
    {
        public int UserId { get; set; }
        public List<CheckoutItemDto> Items { get; set; } = new();
    }
}
