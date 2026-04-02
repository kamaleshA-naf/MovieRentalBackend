namespace MovieRentalApp.Models.DTOs
{
    public class RevenueDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalPayments { get; set; }
        public int CompletedPayments { get; set; }
        public int FailedPayments { get; set; }
    }
}
