namespace MovieRentalApp.Models.DTOs
{
    public class GenreRatingDto
    {
        public string GenreName { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
    }
}