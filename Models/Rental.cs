namespace MovieRentalApp.Models
{
    public class Rental
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;
        public DateTime RentalDate { get; set; }
        public DateTime ExpiryDate { get; set; }

       
        public string StoredStatus { get; set; } = "Active";

        
        public string Status =>
            StoredStatus == "Returned"
                ? "Returned"
                : ExpiryDate < DateTime.UtcNow
                    ? "Expired"
                    : "Active";
    }
}