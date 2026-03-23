namespace MovieRentalApp.Models.DTOs
{
    public class MovieRatingSummaryDto
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int NotForMeCount { get; set; }
        public int LikeCount { get; set; }
        public int LoveCount { get; set; }
    }
}