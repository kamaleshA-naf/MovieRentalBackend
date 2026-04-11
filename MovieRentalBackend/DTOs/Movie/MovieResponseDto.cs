namespace MovieRentalApp.Models.DTOs
{
    public class MovieResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal RentalPrice { get; set; }
        public string Director { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public double Rating { get; set; }
        public bool IsActive { get; set; }
        public string Language { get; set; } = string.Empty;
        public string? VideoUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<GenreResponseDto> Genres { get; set; } = new();

        //extra 
        public string? Hero { get; set; }
        public string? Villain { get; set; }
        public string? Plot { get; set; }
        public string? Summary { get; set; }
    }
}