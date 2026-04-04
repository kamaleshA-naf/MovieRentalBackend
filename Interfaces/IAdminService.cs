using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IAdminService
    {
        Task<DashboardStatsDto> GetDashboardStats();
        Task<IEnumerable<UserRentalSummaryDto>> GetAllUsersWithRentals();
        
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
