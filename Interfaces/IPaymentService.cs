using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> GetPayment(int id);
        Task<IEnumerable<PaymentResponseDto>> GetPaymentsByUser(int userId);
        Task<IEnumerable<PaymentResponseDto>> GetAllPayments();
        Task<PaymentResponseDto> UpdatePaymentStatus(int id, string status);
    }
}