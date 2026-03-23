namespace MovieRentalApp.Models.DTOs
{
    public class UserGenrePreferenceDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public List<GenreRatingDto> GenrePreferences { get; set; } = new();
    }
}