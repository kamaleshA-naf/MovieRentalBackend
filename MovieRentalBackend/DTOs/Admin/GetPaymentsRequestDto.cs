namespace MovieRentalApp.Models.DTOs
{
    public class GetPaymentsRequestDto
    {
        public int     PageNumber    { get; set; } = 1;
        public int     PageSize      { get; set; } = 20;
        public string  SortBy        { get; set; } = "paymentdate";
        public string  SortDirection { get; set; } = "desc";
        public string? Status        { get; set; }  // Completed | Failed | Refunded
        public string? Method        { get; set; }  // UPI | Card | NetBanking
    }
}
