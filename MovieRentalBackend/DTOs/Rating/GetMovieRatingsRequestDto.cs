namespace MovieRentalApp.Models.DTOs
{
    public class GetMovieRatingsRequestDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize   { get; set; } = 10;
    }
}
