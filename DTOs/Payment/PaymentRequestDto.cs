using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models.DTOs
{
    public class PaymentRequestDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int RentalId { get; set; }

        [Required]
        public string Method { get; set; } = string.Empty;
    }
}