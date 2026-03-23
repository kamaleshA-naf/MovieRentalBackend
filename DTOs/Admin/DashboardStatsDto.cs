namespace MovieRentalApp.Models.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalMovies { get; set; }
        public int TotalRentals { get; set; }
        public int ActiveRentals { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalPayments { get; set; }
    }
}