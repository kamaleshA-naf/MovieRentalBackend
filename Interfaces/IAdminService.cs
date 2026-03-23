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
    }
}