using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models.DTOs
{
    public class MovieRatingCreateDto
    {
        // UserId is NOT accepted from the client — it's extracted from JWT in the controller
        [Required]
        [Range(1, 3, ErrorMessage = "Rating must be 1 (Not for me), 2 (I like this), or 3 (Love this!).")]
        public int RatingValue { get; set; }
    }
}