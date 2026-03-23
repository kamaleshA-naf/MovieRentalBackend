using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models.DTOs
{
    public class MovieRatingCreateDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 3, ErrorMessage = "Rating must be 1, 2, or 3.")]
        public int RatingValue { get; set; }
    }
}