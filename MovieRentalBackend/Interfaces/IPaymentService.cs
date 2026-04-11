using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IPaymentService
    {
        Task<PagedResultDto<PaymentResponseDto>> GetPaymentsByUser(
            int userId, GetPaymentsByUserRequestDto request);
    }
}
