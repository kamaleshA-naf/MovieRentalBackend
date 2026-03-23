using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models
{
    public class Cart
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        public int DurationDays { get; set; } = 7;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}