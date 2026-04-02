using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IAdminService
    {
        Task<DashboardStatsDto> GetDashboardStats();
        Task<IEnumerable<UserRentalSummaryDto>> GetAllUsersWithRentals();
        // TODO: remove or implement — GetAllLogs (non-paginated) was removed from the interface
        // but still exists in AdminService. Use GetLogsPaginatedAsync instead.
        //Task<IEnumerable<AuditLogResponseDto>> GetAllLogs();
        Task<RevenueDto> GetRevenueSummary();

        Task<PagedResultDto<PaymentDetailDto>> GetPaymentsPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? status, string? method);

        Task<PagedResultDto<AuditLogResponseDto>> GetLogsPaginatedAsync(
            int pageNumber, int pageSize,
            string sortBy, string sortDirection,
            string? search);
    }
}
