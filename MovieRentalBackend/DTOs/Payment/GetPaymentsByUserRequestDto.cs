namespace MovieRentalApp.Models.DTOs
{
    public class GetPaymentsByUserRequestDto
    {
        public int    PageNumber { get; set; } = 1;
        public int    PageSize   { get; set; } = 10;

        // "asc" = older first, "desc" = recent first (default)
        public string SortOrder  { get; set; } = "desc";
    }
}
