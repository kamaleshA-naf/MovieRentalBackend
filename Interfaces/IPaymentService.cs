using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentResponseDto>> GetPaymentsByUser(int userId);
    }
}
