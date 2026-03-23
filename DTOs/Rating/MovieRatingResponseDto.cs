namespace MovieRentalApp.Models.DTOs
{
    public class MovieRatingResponseDto
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int RatingValue { get; set; }
        public string RatingLabel { get; set; } = string.Empty;
        public DateTime RatedAt { get; set; }
        public bool IsRemoved { get; set; }
    }
}