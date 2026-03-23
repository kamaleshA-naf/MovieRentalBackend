using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models.DTOs
{
    public class CartCheckoutDto
    {
        [Required]
        public int UserId { get; set; }
    }
}