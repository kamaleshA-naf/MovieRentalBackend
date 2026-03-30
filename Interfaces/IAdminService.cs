using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IAdminService
    {
        Task<DashboardStatsDto> GetDashboardStats();
        Task<IEnumerable<UserRentalSummaryDto>> GetAllUsersWithRentals();
        Task<PaymentSummaryDto> GetAllPayments();
        Task<IEnumerable<PaymentDetailDto>> GetPaymentsByUser(int userId);
        Task<IEnumerable<AuditLogResponseDto>> GetAllLogs();
        Task<IEnumerable<AuditLogResponseDto>> GetLogsByUser(int userId);
        Task<AuditLogResponseDto> CreateLog(int userId, string message, string errorNumber);
        Task<RevenueDto> GetRevenueSummary();

        // Paginated + filtered + sortable
        Task<PagedResultDto<UserResponseDto>> GetUsersPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? search, string? role);

        Task<PagedResultDto<PaymentDetailDto>> GetPaymentsPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? status, string? method);

        Task<PagedResultDto<RentalResponseDto>> GetRentalsPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? status);

        Task<PagedResultDto<AuditLogResponseDto>> GetLogsPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? search);

        Task<PagedResultDto<MovieResponseDto>> GetMoviesPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? search, int? genreId, string? language,
            bool? isActive);
    }
}
