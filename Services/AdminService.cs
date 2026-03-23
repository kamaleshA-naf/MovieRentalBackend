using Microsoft.EntityFrameworkCore;
using MovieRentalApp.Contexts;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Services
{
    public class AdminService : IAdminService
    {
        private readonly IRepository<int, User> _userRepository;
        private readonly IRepository<int, Movie> _movieRepository;
        private readonly IRepository<int, Rental> _rentalRepository;
        private readonly IRepository<int, Payment> _paymentRepository;
        private readonly IRepository<int, AuditLog> _auditLogRepository;
        private readonly MovieContext _context;

        public AdminService(
            IRepository<int, User> userRepository,
            IRepository<int, Movie> movieRepository,
            IRepository<int, Rental> rentalRepository,
            IRepository<int, Payment> paymentRepository,
            IRepository<int, AuditLog> auditLogRepository,
            MovieContext context)
        {
            _userRepository = userRepository;
            _movieRepository = movieRepository;
            _rentalRepository = rentalRepository;
            _paymentRepository = paymentRepository;
            _auditLogRepository = auditLogRepository;
            _context = context;
        }

        public async Task<DashboardStatsDto> GetDashboardStats()
        {
            var users = await _userRepository.GetAllAsync();
            var movies = await _movieRepository.GetAllAsync();
            var rentals = await _rentalRepository.GetAllAsync();
            var payments = await _paymentRepository.GetAllAsync();

            return new DashboardStatsDto
            {
                TotalUsers = users.Count(),
                TotalMovies = movies.Count(),
                TotalRentals = rentals.Count(),
                ActiveRentals = rentals.Count(r => r.Status == "Active"),
                TotalRevenue = payments
                    .Where(p => p.Status == "Completed")
                    .Sum(p => p.Amount),
                TotalPayments = payments.Count()
            };
        }

        public async Task<IEnumerable<UserRentalSummaryDto>>
            GetAllUsersWithRentals()
        {
            var users = await _userRepository.GetAllAsync();
            var rentals = await _rentalRepository
                .GetAllWithIncludeAsync(r => r.Movie);

            return users.Select(u => new UserRentalSummaryDto
            {
                UserId = u.UserId,
                UserName = u.UserName,
                Email = u.UserEmail,
                Role = u.Role.ToString(),
                TotalRentals = rentals.Count(r => r.UserId == u.UserId),
                Rentals = rentals
                    .Where(r => r.UserId == u.UserId)
                    .Select(r => new RentalResponseDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserName = u.UserName,
                        MovieId = r.MovieId,
                        MovieTitle = r.Movie?.Title ?? "",
                        RentalDate = r.RentalDate,
                        ExpiryDate = r.ExpiryDate,
                        Status = r.Status
                    }).ToList()
            });
        }

        public async Task<PaymentSummaryDto> GetAllPayments()
        {
            var payments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Movie)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return new PaymentSummaryDto
            {
                TotalRevenue = payments
                    .Where(p => p.Status == "Completed")
                    .Sum(p => p.Amount),
                TotalPayments = payments.Count,
                Payments = payments.Select(MapPayment).ToList()
            };
        }

        public async Task<IEnumerable<PaymentDetailDto>>
            GetPaymentsByUser(int userId)
        {
            var payments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Movie)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapPayment);
        }

        public async Task<IEnumerable<AuditLogResponseDto>> GetAllLogs()
        {
            var logs = await _auditLogRepository
                .GetAllWithIncludeAsync(a => a.User);
            return logs.OrderByDescending(a => a.CreatedAt)
                       .Select(MapLog);
        }

        public async Task<IEnumerable<AuditLogResponseDto>>
            GetLogsByUser(int userId)
        {
            var logs = await _auditLogRepository
                .FindAsync(a => a.UserId == userId);
            return logs.OrderByDescending(a => a.CreatedAt)
                       .Select(MapLog);
        }

        public async Task<AuditLogResponseDto> CreateLog(
            int userId, string message, string errorNumber)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException("User", userId);

            var log = new AuditLog
            {
                UserId = userId,
                UserName = user.UserName,
                Role = user.Role.ToString(),
                Message = message,
                ErrorNumber = errorNumber,
                CreatedAt = DateTime.UtcNow
            };
            await _auditLogRepository.AddAsync(log);
            return MapLog(log);
        }

        public async Task<RevenueDto> GetRevenueSummary()
        {
            var payments = await _context.Payments
                .Include(p => p.Movie)
                .Include(p => p.User)
                .ToListAsync();

            var completed = payments
                .Where(p => p.Status == "Completed").ToList();
            var refunded = payments
                .Where(p => p.Status == "Refunded").ToList();

            var byMovie = completed
                .GroupBy(p => new
                {
                    p.MovieId,
                    Title = p.Movie?.Title ?? "Unknown"
                })
                .Select(g => new RevenueByMovieDto
                {
                    MovieId = g.Key.MovieId,
                    MovieTitle = g.Key.Title,
                    TotalRevenue = g.Sum(p => p.Amount),
                    TotalRentals = g.Count()
                })
                .OrderByDescending(m => m.TotalRevenue)
                .Take(10).ToList();

            var byMonth = completed
                .GroupBy(p => new
                {
                    p.PaymentDate.Year,
                    p.PaymentDate.Month
                })
                .Select(g => new RevenueByMonthDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Label = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Revenue = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month).ToList();

            return new RevenueDto
            {
                TotalRevenue = completed.Sum(p => p.Amount),
                CompletedRevenue = completed.Sum(p => p.Amount),
                RefundedAmount = refunded.Sum(p => p.Amount),
                NetRevenue = completed.Sum(p => p.Amount)
                                  - refunded.Sum(p => p.Amount),
                TotalPayments = payments.Count,
                CompletedPayments = completed.Count,
                RefundedPayments = refunded.Count,
                PendingPayments = payments.Count(p => p.Status == "Pending"),
                FailedPayments = payments.Count(p => p.Status == "Failed"),
                TopMovies = byMovie,
                ByMonth = byMonth,
                Payments = payments
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(MapPayment).ToList()
            };
        }

        private static PaymentDetailDto MapPayment(Payment p) => new()
        {
            Id = p.Id,
            UserId = p.UserId,
            UserName = p.User?.UserName ?? "Unknown",
            MovieId = p.MovieId,
            MovieTitle = p.Movie?.Title ?? "Unknown",
            RentalId = p.RentalId,
            Amount = p.Amount,
            Method = p.Method,
            Status = p.Status,
            PaidAt = p.PaymentDate
        };

        private static AuditLogResponseDto MapLog(AuditLog a) => new()
        {
            LogId = a.LogId,
            UserId = a.UserId,
            UserName = a.UserName,
            Role = a.Role,
            Message = a.Message,
            ErrorNumber = a.ErrorNumber,
            CreatedAt = a.CreatedAt
        };
    }
}