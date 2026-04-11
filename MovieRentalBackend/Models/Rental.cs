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
        public DateTime? ReturnDate { get; set; }

        // Single source of truth — stored in DB as "Active" | "Expired" | "Returned"
        public string StoredStatus { get; set; } = "Active";

        // Computed for reading — auto-upgrades Active→Expired if past expiry date
        // StoredStatus is updated to "Expired" by the sync endpoint
        public string Status =>
            StoredStatus == "Returned"
                ? "Returned"
                : StoredStatus == "Expired" || ExpiryDate < DateTime.UtcNow
                    ? "Expired"
                    : "Active";
    }
}
