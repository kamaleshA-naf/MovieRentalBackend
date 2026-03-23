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

        public async Task<PaymentResponseDto> GetPayment(int id)
        {
            if (id <= 0)
                throw new BusinessRuleViolationException(
                    "Invalid payment ID.");

            var payments = await _paymentRepository
                .GetAllWithIncludeAsync(p => p.User, p => p.Movie);
            var payment = payments.FirstOrDefault(p => p.Id == id);
            if (payment == null)
                throw new EntityNotFoundException("Payment", id);

            return MapToDto(payment);
        }

        public async Task<IEnumerable<PaymentResponseDto>>
            GetPaymentsByUser(int userId)
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

        public async Task<IEnumerable<PaymentResponseDto>>
            GetAllPayments()
        {
            var payments = await _paymentRepository
                .GetAllWithIncludeAsync(p => p.User, p => p.Movie);

            return payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(MapToDto);
        }

        public async Task<PaymentResponseDto> UpdatePaymentStatus(
            int id, string status)
        {
            if (id <= 0)
                throw new BusinessRuleViolationException(
                    "Invalid payment ID.");

            var validStatuses = new[]
            {
                "Completed", "Failed", "Refunded", "Pending"
            };
            if (!validStatuses.Contains(status))
                throw new BusinessRuleViolationException(
                    $"Invalid status. Valid: " +
                    string.Join(", ", validStatuses));

            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
                throw new EntityNotFoundException("Payment", id);

            if (payment.Status == "Refunded")
                throw new BusinessRuleViolationException(
                    "Cannot update a refunded payment.");

            payment.Status = status;
            await _paymentRepository.UpdateAsync(id, payment);

            var payments = await _paymentRepository
                .GetAllWithIncludeAsync(p => p.User, p => p.Movie);
            var updated = payments.FirstOrDefault(p => p.Id == id);
            return MapToDto(updated!);
        }

        private static PaymentResponseDto MapToDto(Payment p) => new()
        {
            Id = p.Id,
            UserId = p.UserId,
            UserName = p.User?.UserName ?? "",
            MovieId = p.MovieId,
            MovieTitle = p.Movie?.Title ?? "",
            RentalId = p.RentalId,
            Amount = p.Amount,
            Method = p.Method,
            Status = p.Status,
            PaidAt = p.PaymentDate
        };
    }
}