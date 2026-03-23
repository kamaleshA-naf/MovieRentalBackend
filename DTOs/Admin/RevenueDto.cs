namespace MovieRentalApp.Models.DTOs
{
    public class RevenueDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal CompletedRevenue { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal NetRevenue { get; set; }
        public int TotalPayments { get; set; }
        public int CompletedPayments { get; set; }
        public int RefundedPayments { get; set; }
        public int PendingPayments { get; set; }
        public int FailedPayments { get; set; }
        public List<RevenueByMovieDto> TopMovies { get; set; } = new();
        public List<RevenueByMonthDto> ByMonth { get; set; } = new();
        public List<PaymentDetailDto> Payments { get; set; } = new();
    }

    public class RevenueByMovieDto
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalRentals { get; set; }
    }

    public class RevenueByMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Count { get; set; }
    }
}