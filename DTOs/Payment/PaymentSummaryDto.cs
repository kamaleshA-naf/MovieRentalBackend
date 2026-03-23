namespace MovieRentalApp.Models.DTOs
{
    public class PaymentSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalPayments { get; set; }
        public List<PaymentDetailDto> Payments { get; set; } = new();
    }
}