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
        private readonly IRepository<int, AuditLog> _auditLogRepository;
        private readonly MovieContext _context;

        public AdminService(
            IRepository<int, User> userRepository,
            IRepository<int, Movie> movieRepository,
            IRepository<int, Rental> rentalRepository,
            IRepository<int, AuditLog> auditLogRepository,
            MovieContext context)
        {
            _userRepository = userRepository;
            _movieRepository = movieRepository;
            _rentalRepository = rentalRepository;
            _auditLogRepository = auditLogRepository;
            _context = context;
        }

        // ── DASHBOARD ─────────────────────────────────────────────
        public async Task<DashboardStatsDto> GetDashboardStats()
        {
            var totalUsers    = await _context.Users.CountAsync();
            var totalMovies   = await _context.Movies.CountAsync();
            var totalRentals  = await _context.Rentals.CountAsync();
            var activeRentals = await _context.Rentals
                .CountAsync(r => r.StoredStatus == "Active" && r.ExpiryDate > DateTime.Now);
            var totalRevenue  = await _context.Payments
                .Where(p => p.Status == "Completed" && p.Amount > 0)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            var totalPayments = await _context.Payments.CountAsync();

            var recentPayments = await _context.Payments
                .Include(p => p.User).Include(p => p.Movie)
                .Where(p => p.Amount > 0)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5).ToListAsync();

            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5).ToListAsync();

            return new DashboardStatsDto
            {
                TotalUsers    = totalUsers,
                TotalMovies   = totalMovies,
                TotalRentals  = totalRentals,
                ActiveRentals = activeRentals,
                TotalRevenue  = totalRevenue,
                TotalPayments = totalPayments,
                RecentPayments = recentPayments.Select(MapPayment).ToList(),
                RecentUsers = recentUsers.Select(MapUser).ToList()
            };
        }

        // ── USERS WITH RENTALS ────────────────────────────────────
        public async Task<IEnumerable<UserRentalSummaryDto>> GetAllUsersWithRentals()
        {
            var users   = await _userRepository.GetAllAsync();
            var rentals = await _rentalRepository.GetAllWithIncludeAsync(r => r.Movie);

            return users.Select(u => new UserRentalSummaryDto
            {
                UserId       = u.UserId,
                UserName     = u.UserName,
                Email        = u.UserEmail,
                Role         = u.Role.ToString(),
                TotalRentals = rentals.Count(r => r.UserId == u.UserId),
                CreatedAt    = u.CreatedAt,
                Rentals = rentals.Where(r => r.UserId == u.UserId)
                    .Select(r => new RentalResponseDto
                    {
                        Id = r.Id, UserId = r.UserId, UserName = u.UserName,
                        MovieId = r.MovieId, MovieTitle = r.Movie?.Title ?? "",
                        RentalDate = r.RentalDate, ExpiryDate = r.ExpiryDate, Status = r.Status
                    }).ToList()
            });
        }

        // ── LOGS ──────────────────────────────────────────────────
       
        public async Task<IEnumerable<AuditLogResponseDto>> GetAllLogs()
        {
            var logs = await _auditLogRepository.GetAllWithIncludeAsync(a => a.User);
            return logs.OrderByDescending(a => a.CreatedAt).Select(MapLog);
        }

        // ── REVENUE ───────────────────────────────────────────────
        public async Task<RevenueDto> GetRevenueSummary()
        {
            // Only aggregate counts — no joins, no GroupBy, no full list load
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == "Completed" && p.Amount > 0)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var totalPayments     = await _context.Payments.CountAsync();
            var completedPayments = await _context.Payments.CountAsync(p => p.Status == "Completed");
            var failedPayments    = await _context.Payments.CountAsync(p => p.Status == "Failed");

            return new RevenueDto
            {
                TotalRevenue      = totalRevenue,
                TotalPayments     = totalPayments,
                CompletedPayments = completedPayments,
                FailedPayments    = failedPayments
            };
        }

        // ── PAGINATED PAYMENTS (status + method filter) ───────────
        public async Task<PagedResultDto<PaymentDetailDto>> GetPaymentsPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? status, string? method)
        {
            var query = _context.Payments.Include(p => p.User).Include(p => p.Movie).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(p => p.Status.ToLower() == status.ToLower());
            if (!string.IsNullOrWhiteSpace(method))
                query = query.Where(p => p.Method.ToLower() == method.ToLower());

            query = (sortBy?.ToLower(), sortDirection?.ToLower() == "desc") switch
            {
                ("amount",      true)  => query.OrderByDescending(p => p.Amount),
                ("amount",      false) => query.OrderBy(p => p.Amount),
                ("status",      true)  => query.OrderByDescending(p => p.Status),
                ("status",      false) => query.OrderBy(p => p.Status),
                ("paymentdate", false) => query.OrderBy(p => p.PaymentDate),
                _                      => query.OrderByDescending(p => p.PaymentDate)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return BuildPaged(items.Select(MapPayment).ToList(), total, pageNumber, pageSize);
        }

        // ── PAGINATED LOGS (search + sort) ────────────────────────
        public async Task<PagedResultDto<AuditLogResponseDto>> GetLogsPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? search)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.ToLower();
                query = query.Where(a =>
                    a.UserName.ToLower().Contains(kw) ||
                    a.Message.ToLower().Contains(kw) ||
                    a.Role.ToLower().Contains(kw));
            }

            query = (sortBy?.ToLower(), sortDirection?.ToLower() == "desc") switch
            {
                ("username",  true)  => query.OrderByDescending(a => a.UserName),
                ("username",  false) => query.OrderBy(a => a.UserName),
                ("createdat", false) => query.OrderBy(a => a.CreatedAt),
                _                    => query.OrderByDescending(a => a.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return BuildPaged(items.Select(MapLog).ToList(), total, pageNumber, pageSize);
        }

        // ── MAPPERS ───────────────────────────────────────────────
        private static PaymentDetailDto MapPayment(Payment p) => new()
        {
            Id             = p.Id,
            UserId         = p.UserId,
            UserName       = p.User?.UserName ?? "Unknown",
            MovieId        = p.MovieId,
            MovieTitle     = p.Movie?.Title ?? "Unknown",
            RentalId       = p.RentalId,
            Amount         = p.Amount < 0 ? 0 : p.Amount,
            RefundedAmount = p.Amount < 0 ? Math.Abs(p.Amount) : 0,
            Method         = p.Method,
            Status         = p.Status,
            PaidAt         = p.PaymentDate
        };

        private static AuditLogResponseDto MapLog(AuditLog a) => new()
        {
            Id          = a.LogId,
            LogId       = a.LogId,
            UserId      = a.UserId,
            UserName    = a.UserName,
            Role        = a.Role,
            Message     = a.Message,
            ErrorNumber = a.ErrorNumber,
            CreatedAt   = a.CreatedAt
        };

        private static UserResponseDto MapUser(User u) => new()
        {
            Id        = u.UserId,
            Name      = u.UserName,
            Email     = u.UserEmail,
            Role      = u.Role.ToString(),
            IsActive  = u.IsActive,
            CreatedAt = u.CreatedAt
        };

        private static PagedResultDto<T> BuildPaged<T>(
            List<T> items, int total, int pageNumber, int pageSize)
        {
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            return new PagedResultDto<T>
            {
                Data        = items,
                TotalCount  = total,
                PageNumber  = pageNumber,
                PageSize    = pageSize,
                TotalPages  = totalPages,
                HasNext     = pageNumber < totalPages,
                HasPrevious = pageNumber > 1
            };
        }
    }
}
