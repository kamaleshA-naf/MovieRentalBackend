using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IRepository<int, Payment> _paymentRepository;
        private readonly IRepository<int, User> _userRepository;

        public PaymentService(
            IRepository<int, Payment> paymentRepository,
            IRepository<int, User> userRepository)
        {
            _paymentRepository = paymentRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<PaymentResponseDto>> GetPaymentsByUser(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException("User", userId);

            var payments = await _paymentRepository
                .GetAllWithIncludeAsync(p => p.User, p => p.Movie);

            return payments
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .Select(MapToDto);
        }

        private static PaymentResponseDto MapToDto(Payment p) => new()
        {
            Id = p.Id,
            UserId = p.UserId,
            UserName = p.User?.UserName ?? "",
            MovieId = p.MovieId,
            MovieTitle = p.Movie?.Title ?? "",
            RentalId = p.RentalId,
            Amount = p.Amount < 0 ? 0 : p.Amount,
            Method = p.Method,
            Status = p.Status,
            PaidAt = p.PaymentDate
        };
    }
}
