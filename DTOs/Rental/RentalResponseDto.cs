namespace MovieRentalApp.Models.DTOs
{
    public class RentalResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public DateTime RentalDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool CanReturn { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal RentalPrice { get; set; }   // price per day
        public decimal TotalPaid { get; set; }     // total amount paid
        public bool MovieIsActive { get; set; }    // false = movie deleted by admin
    }
}
