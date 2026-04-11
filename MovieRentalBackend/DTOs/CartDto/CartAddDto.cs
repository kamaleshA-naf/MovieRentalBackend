using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models.DTOs
{
    public class CartAddDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int MovieId { get; set; }

        public int DurationDays { get; set; } = 7;
    }
}