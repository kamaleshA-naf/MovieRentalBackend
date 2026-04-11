namespace MovieRentalApp.Models.DTOs
{
    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int RentalId { get; set; }
        public decimal Amount { get; set; }          // original paid amount (never negative)
        public decimal RefundedAmount { get; set; }  // positive refund value (0 if none)
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime PaidAt { get; set; }
    }
}
