using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models.DTOs
{
    public class MovieCreateDto
    {
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0.01, double.MaxValue,
            ErrorMessage = "Rental price must be greater than zero.")]
        public decimal RentalPrice { get; set; }

        public string? Director { get; set; }
        public int ReleaseYear { get; set; }
        public double Rating { get; set; }

        [Required(ErrorMessage = "Language is required.")]
        [MinLength(1, ErrorMessage = "Language cannot be empty.")]
        public string Language { get; set; } = string.Empty;

        public List<int>? GenreIds { get; set; }
        public string? VideoUrl { get; set; }
        public string? ThumbnailUrl { get; set; }

        //extra
        public string? Hero { get; set; }
        public string? Villain { get; set; }
        public string? Plot { get; set; }
        public string? Summary { get; set; }
    }
}