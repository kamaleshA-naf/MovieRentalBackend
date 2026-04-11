namespace MovieRentalApp.Models.DTOs
{
    public class GetMoviesRequestDto
    {
        public int     PageNumber    { get; set; } = 1;
        public int     PageSize      { get; set; } = 20;
        public int?    GenreId       { get; set; }
        public string? Language      { get; set; }
        public double? MinRating     { get; set; }
        public string? Status        { get; set; }  // "released" | "coming_soon" | null = all
        public string  SortBy        { get; set; } = "Id";
        public string  SortDirection { get; set; } = "desc";
    }
}
