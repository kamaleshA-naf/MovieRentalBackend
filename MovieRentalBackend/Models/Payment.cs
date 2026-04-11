namespace MovieRentalApp.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

       
        public int RentalId { get; set; }

        
        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = "Completed";
        public string? FailureReason { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}