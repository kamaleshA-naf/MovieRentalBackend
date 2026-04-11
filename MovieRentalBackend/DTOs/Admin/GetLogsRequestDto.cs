namespace MovieRentalApp.Models.DTOs
{
    public class GetLogsRequestDto
    {
        public int     PageNumber    { get; set; } = 1;
        public int     PageSize      { get; set; } = 20;
        public string  SortBy        { get; set; } = "createdat";
        public string  SortDirection { get; set; } = "desc";
        public string? Search        { get; set; }
    }
}
