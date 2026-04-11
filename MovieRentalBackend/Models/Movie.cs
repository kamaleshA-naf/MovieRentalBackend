namespace MovieRentalApp.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal RentalPrice { get; set; }
        public string Director { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public double Rating { get; set; }
        public bool IsActive { get; set; } = true;
        public string Language { get; set; } = string.Empty;
        public string? VideoUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int ViewCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Hero { get; set; }
        public string? Villain { get; set; }
        public string? Plot { get; set; }
        public string? Summary { get; set; }

        public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
        public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    }
}
