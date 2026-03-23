using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models
{
    public class MovieRating
    {
        public int Id { get; set; }

        [Required]
        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // 1 = Not for me, 2 = I like this, 3 = Love this!
        [Range(1, 3)]
        public int RatingValue { get; set; }

        public DateTime RatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Soft delete — if true, rating is removed but record kept for analytics
        public bool IsRemoved { get; set; } = false;
    }
}