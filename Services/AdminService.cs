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

        // ── DASHBOARD ─────────────────────────────────────────────
        public async Task<DashboardStatsDto> GetDashboardStats()
        {
            var totalUsers    = await _context.Users.CountAsync();
            var totalMovies   = await _context.Movies.CountAsync();
            var totalRentals  = await _context.Rentals.CountAsync();
            var activeRentals = await _context.Rentals
                .CountAsync(r => r.StoredStatus == "Active" && r.ExpiryDate > DateTime.Now);

            var totalRevenue = await _context.Payments
                .Where(p => p.Status == "Completed" && p.Amount > 0)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var totalPayments = await _context.Payments.CountAsync();

            var recentPayments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Movie)
                .Where(p => p.Amount > 0)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();

            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

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
                Rentals = rentals
                    .Where(r => r.UserId == u.UserId)
                    .Select(r => new RentalResponseDto
                    {
                        Id         = r.Id,
                        UserId     = r.UserId,
                        UserName   = u.UserName,
                        MovieId    = r.MovieId,
                        MovieTitle = r.Movie?.Title ?? "",
                        RentalDate = r.RentalDate,
                        ExpiryDate = r.ExpiryDate,
                        Status     = r.Status
                    }).ToList()
            });
        }

        // ── ALL PAYMENTS (non-paginated, for revenue widget) ──────
        public async Task<PaymentSummaryDto> GetAllPayments()
        {
            var payments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Movie)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var originals = payments.Where(p => p.Amount >= 0).ToList();

            return new PaymentSummaryDto
            {
                TotalRevenue  = originals.Where(p => p.Status == "Completed").Sum(p => p.Amount),
                TotalPayments = originals.Count,
                Payments      = payments.Select(MapPayment).ToList()
            };
        }

        public async Task<IEnumerable<PaymentDetailDto>> GetPaymentsByUser(int userId)
        {
            var payments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Movie)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapPayment);
        }

        // ── LOGS (non-paginated) ──────────────────────────────────
        public async Task<IEnumerable<AuditLogResponseDto>> GetAllLogs()
        {
            var logs = await _auditLogRepository.GetAllWithIncludeAsync(a => a.User);
            return logs.OrderByDescending(a => a.CreatedAt).Select(MapLog);
        }

        public async Task<IEnumerable<AuditLogResponseDto>> GetLogsByUser(int userId)
        {
            var logs = await _auditLogRepository.FindAsync(a => a.UserId == userId);
            return logs.OrderByDescending(a => a.CreatedAt).Select(MapLog);
        }

        public async Task<AuditLogResponseDto> CreateLog(
            int userId, string message, string errorNumber)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("User", userId);

            var log = new AuditLog
            {
                UserId      = userId,
                UserName    = user.UserName,
                Role        = user.Role.ToString(),
                Message     = message,
                ErrorNumber = errorNumber,
                CreatedAt   = DateTime.UtcNow
            };
            await _auditLogRepository.AddAsync(log);
            return MapLog(log);
        }

        // ── REVENUE ───────────────────────────────────────────────
        public async Task<RevenueDto> GetRevenueSummary()
        {
            var payments  = await _context.Payments.Include(p => p.Movie).Include(p => p.User).ToListAsync();
            var completed = payments.Where(p => p.Status == "Completed").ToList();
            var refunded  = payments.Where(p => p.Status == "Refunded").ToList();

            var byMovie = completed
                .GroupBy(p => new { p.MovieId, Title = p.Movie?.Title ?? "Unknown" })
                .Select(g => new RevenueByMovieDto
                {
                    MovieId      = g.Key.MovieId,
                    MovieTitle   = g.Key.Title,
                    TotalRevenue = g.Sum(p => p.Amount),
                    TotalRentals = g.Count()
                })
                .OrderByDescending(m => m.TotalRevenue).Take(10).ToList();

            var byMonth = completed
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new RevenueByMonthDto
                {
                    Year    = g.Key.Year,
                    Month   = g.Key.Month,
                    Label   = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Revenue = g.Sum(p => p.Amount),
                    Count   = g.Count()
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month).ToList();

            return new RevenueDto
            {
                TotalRevenue      = completed.Sum(p => p.Amount),
                CompletedRevenue  = completed.Sum(p => p.Amount),
                RefundedAmount    = refunded.Sum(p => p.Amount),
                NetRevenue        = completed.Sum(p => p.Amount) - refunded.Sum(p => p.Amount),
                TotalPayments     = payments.Count,
                CompletedPayments = completed.Count,
                RefundedPayments  = refunded.Count,
                PendingPayments   = payments.Count(p => p.Status == "Pending"),
                FailedPayments    = payments.Count(p => p.Status == "Failed"),
                TopMovies         = byMovie,
                ByMonth           = byMonth,
                Payments          = payments.OrderByDescending(p => p.PaymentDate).Select(MapPayment).ToList()
            };
        }

        // ── TODAY'S USERS — REMOVED ───────────────────────────────

        // ── PAGINATED USERS (search + role filter) ────────────────
        public async Task<PagedResultDto<UserResponseDto>> GetUsersPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? search, string? role)
        {
            var query = _context.Users.AsQueryable();

            // Filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.ToLower();
                query = query.Where(u =>
                    u.UserName.ToLower().Contains(kw) ||
                    u.UserEmail.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Role.ToString().ToLower() == role.ToLower());

            // Sorting
            query = (sortBy?.ToLower(), sortDirection?.ToLower() == "desc") switch
            {
                ("createdat", true)  => query.OrderByDescending(u => u.CreatedAt),
                ("createdat", false) => query.OrderBy(u => u.CreatedAt),
                ("name",      true)  => query.OrderByDescending(u => u.UserName),
                ("name",      false) => query.OrderBy(u => u.UserName),
                ("username",  true)  => query.OrderByDescending(u => u.UserName),
                _                    => query.OrderBy(u => u.UserName)
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserResponseDto
                {
                    Id        = u.UserId,
                    Name      = u.UserName,
                    Email     = u.UserEmail,
                    Role      = u.Role.ToString(),
                    IsActive  = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return BuildPaged(items, total, pageNumber, pageSize);
        }

        // ── PAGINATED PAYMENTS (status + method filter) ───────────
        public async Task<PagedResultDto<PaymentDetailDto>> GetPaymentsPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? status, string? method)
        {
            var query = _context.Payments
                .Include(p => p.User)
                .Include(p => p.Movie)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(p => p.Status.ToLower() == status.ToLower());

            if (!string.IsNullOrWhiteSpace(method))
                query = query.Where(p => p.Method.ToLower() == method.ToLower());

            // Sorting
            query = (sortBy?.ToLower(), sortDirection?.ToLower() == "desc") switch
            {
                ("amount",      true)  => query.OrderByDescending(p => p.Amount),
                ("amount",      false) => query.OrderBy(p => p.Amount),
                ("status",      true)  => query.OrderByDescending(p => p.Status),
                ("status",      false) => query.OrderBy(p => p.Status),
                ("method",      true)  => query.OrderByDescending(p => p.Method),
                ("method",      false) => query.OrderBy(p => p.Method),
                ("paymentdate", false) => query.OrderBy(p => p.PaymentDate),
                _                      => query.OrderByDescending(p => p.PaymentDate)
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return BuildPaged(items.Select(MapPayment).ToList(), total, pageNumber, pageSize);
        }

        // ── PAGINATED RENTALS (status filter) ────────────────────
        public async Task<PagedResultDto<RentalResponseDto>> GetRentalsPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? status)
        {
            var query = _context.Rentals
                .Include(r => r.Movie)
                .Include(r => r.User)
                .AsQueryable();

            // Status filter — compare against computed status logic
            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.ToLower();
                query = s switch
                {
                    "active"   => query.Where(r => r.StoredStatus == "Active" && r.ExpiryDate > DateTime.Now),
                    "expired"  => query.Where(r => r.StoredStatus == "Active" && r.ExpiryDate <= DateTime.Now),
                    "returned" => query.Where(r => r.StoredStatus == "Returned"),
                    _          => query
                };
            }

            // Sorting
            query = (sortBy?.ToLower(), sortDirection?.ToLower() == "desc") switch
            {
                ("rentaldate", false) => query.OrderBy(r => r.RentalDate),
                ("expirydate", true)  => query.OrderByDescending(r => r.ExpiryDate),
                ("expirydate", false) => query.OrderBy(r => r.ExpiryDate),
                _                     => query.OrderByDescending(r => r.RentalDate)
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(r => new RentalResponseDto
            {
                Id           = r.Id,
                UserId       = r.UserId,
                UserName     = r.User?.UserName ?? "",
                MovieId      = r.MovieId,
                MovieTitle   = r.Movie?.Title ?? "Movie Unavailable",
                RentalDate   = r.RentalDate,
                ExpiryDate   = r.ExpiryDate,
                ReturnDate   = r.ReturnDate,
                Status       = r.Status,
                RentalPrice  = r.Movie?.RentalPrice ?? 0,
                MovieIsActive = r.Movie?.IsActive ?? false
            }).ToList();

            return BuildPaged(dtos, total, pageNumber, pageSize);
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
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return BuildPaged(items.Select(MapLog).ToList(), total, pageNumber, pageSize);
        }

        // ── PAGINATED MOVIES (admin — includes inactive) ──────────
        public async Task<PagedResultDto<MovieResponseDto>> GetMoviesPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? search, int? genreId, string? language,
            bool? isActive)
        {
            var query = _context.Movies
                .Include(m => m.MovieGenres)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.ToLower();
                query = query.Where(m =>
                    m.Title.ToLower().Contains(kw) ||
                    m.Director.ToLower().Contains(kw));
            }

            if (genreId.HasValue)
                query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId.Value));

            if (!string.IsNullOrWhiteSpace(language))
                query = query.Where(m => m.Language.ToLower() == language.ToLower());

            if (isActive.HasValue)
                query = query.Where(m => m.IsActive == isActive.Value);

            // Sorting
            query = (sortBy?.ToLower(), sortDirection?.ToLower() == "desc") switch
            {
                ("title",  true)  => query.OrderByDescending(m => m.Title),
                ("title",  false) => query.OrderBy(m => m.Title),
                ("price",  true)  => query.OrderByDescending(m => m.RentalPrice),
                ("price",  false) => query.OrderBy(m => m.RentalPrice),
                ("rating", true)  => query.OrderByDescending(m => m.Rating),
                ("rating", false) => query.OrderBy(m => m.Rating),
                ("views",  true)  => query.OrderByDescending(m => m.ViewCount),
                _                 => query.OrderByDescending(m => m.Id)
            };

            var total = await query.CountAsync();
            var movies = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Batch load genres
            var movieIds   = movies.Select(m => m.Id).ToList();
            var genreLinks = await _context.MovieGenres
                .Include(mg => mg.Genre)
                .Where(mg => movieIds.Contains(mg.MovieId))
                .ToListAsync();

            var dtos = movies.Select(m =>
            {
                var genres = genreLinks
                    .Where(mg => mg.MovieId == m.Id)
                    .Select(mg => new GenreResponseDto
                    {
                        Id   = mg.GenreId,
                        Name = mg.Genre?.Name ?? ""
                    }).ToList();

                return new MovieResponseDto
                {
                    Id          = m.Id,
                    Title       = m.Title,
                    Description = m.Description,
                    RentalPrice = m.RentalPrice,
                    Director    = m.Director,
                    ReleaseYear = m.ReleaseYear,
                    Rating      = m.Rating,
                    IsActive    = m.IsActive,
                    Language    = m.Language,
                    VideoUrl    = m.VideoUrl,
                    ThumbnailUrl = m.ThumbnailUrl,
                    ViewCount   = m.ViewCount,
                    CreatedAt   = m.CreatedAt,
                    Genres      = genres
                };
            }).ToList();

            return BuildPaged(dtos, total, pageNumber, pageSize);
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
            FailureReason  = p.Status == "Failed" ? p.FailureReason : null,
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

        // ── HELPER ────────────────────────────────────────────────
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
