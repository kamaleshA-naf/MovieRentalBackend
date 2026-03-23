using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models.DTOs
{
    public class RentalCreateDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int MovieId { get; set; }

        [Required]
        [Range(1, 30,
            ErrorMessage = "Rental duration must be between 1 and 30 days.")]
        public int DurationDays { get; set; }
    }
}