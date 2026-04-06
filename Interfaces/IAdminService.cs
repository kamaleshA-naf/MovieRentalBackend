using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IAdminService
    {
        Task<DashboardStatsDto>                   GetDashboardStats();
        Task<IEnumerable<UserRentalSummaryDto>>   GetAllUsersWithRentals();
        Task<RevenueDto>                          GetRevenueSummary();
        Task<PagedResultDto<PaymentDetailDto>>    GetPayments(GetPaymentsRequestDto request);
        Task<PagedResultDto<AuditLogResponseDto>> GetLogs(GetLogsRequestDto request);
    }
}
