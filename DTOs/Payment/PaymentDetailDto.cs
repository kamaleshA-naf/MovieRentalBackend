namespace MovieRentalApp.Models.DTOs
{
    public class PaymentDetailDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int RentalId { get; set; }
        public decimal Amount { get; set; }
        public decimal RefundedAmount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public DateTime PaidAt { get; set; }
    }
}
