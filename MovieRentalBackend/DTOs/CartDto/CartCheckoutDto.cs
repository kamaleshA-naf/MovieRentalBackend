using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models.DTOs
{
    public class CartCheckoutDto
    {
        [Required]
        public int UserId { get; set; }

        // UPI / Card / NetBanking
        public string PaymentMethod { get; set; } = "UPI";
    }
}
