namespace MovieRentalApp.Models.DTOs
{
    public class MovieUpdateDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal RentalPrice { get; set; }
        public string? Director { get; set; }
        public int ReleaseYear { get; set; }
        public double Rating { get; set; }
        public string? VideoUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool? IsActive { get; set; }
        public string? Language { get; set; }
        public List<int>? GenreIds { get; set; }


        //extra 

        public string? Hero { get; set; }
        public string? Villain { get; set; }
        public string? Plot { get; set; }
        public string? Summary { get; set; }
    }
}