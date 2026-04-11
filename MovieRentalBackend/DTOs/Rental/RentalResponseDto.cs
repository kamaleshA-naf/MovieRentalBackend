namespace MovieRentalApp.Models.DTOs
{
    public class RentalResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public DateTime RentalDate { get; set; }       // rentedAt
        public DateTime ExpiryDate { get; set; }       // expiresAt
        public DateTime? ReturnDate { get; set; }      // returnedAt
        public string Status { get; set; } = string.Empty;  // Active | Returned | Expired
        public bool IsActive { get; set; }             // true only when Status == "Active"
        public bool CanReturn { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal RentalPrice { get; set; }
        public decimal TotalPaid { get; set; }
        public bool MovieIsActive { get; set; }
    }
}
