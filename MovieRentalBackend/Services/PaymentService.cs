using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using System.Diagnostics;

namespace MovieRentalApp.Services
{
    [DebuggerNonUserCode]
    public class PaymentService : IPaymentService
    {
        private readonly IRepository<int, Payment> _paymentRepository;
        private readonly IRepository<int, User>    _userRepository;

        public PaymentService(
            IRepository<int, Payment> paymentRepository,
            IRepository<int, User>    userRepository)
        {
            _paymentRepository = paymentRepository;
            _userRepository    = userRepository;
        }

        public async Task<PagedResultDto<PaymentResponseDto>> GetPaymentsByUser(
            int userId, GetPaymentsByUserRequestDto request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException("User", userId);

            var all = await _paymentRepository
                .GetAllWithIncludeAsync(p => p.User, p => p.Movie);

            // All payments for this user — Completed, Failed, and Refunded
            var userPayments = all
                .Where(p => p.UserId == userId)
                .ToList();

            // Sort by paymentDate
            var sorted = request.SortOrder.ToLower() == "asc"
                ? userPayments.OrderBy(p => p.PaymentDate).ToList()
                : userPayments.OrderByDescending(p => p.PaymentDate).ToList();

            var totalCount = sorted.Count;
            // Use requested page size but cap at 200 to avoid abuse
            var pageSize   = Math.Min(request.PageSize > 0 ? request.PageSize : 50, 200);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var paged = sorted
                .Skip((request.PageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResultDto<PaymentResponseDto>
            {
                Data        = paged,
                TotalCount  = totalCount,
                PageNumber  = request.PageNumber,
                PageSize    = pageSize,
                TotalPages  = totalPages,
                HasNext     = request.PageNumber < totalPages,
                HasPrevious = request.PageNumber > 1
            };
        }

        private static PaymentResponseDto MapToDto(Payment p) => new()
        {
            Id             = p.Id,
            UserId         = p.UserId,
            UserName       = p.User?.UserName ?? "",
            MovieId        = p.MovieId,
            MovieTitle     = p.Movie?.Title ?? "",
            RentalId       = p.RentalId,
            Amount         = Math.Abs(p.Amount),  // always positive; status tells direction
            RefundedAmount = 0,
            Method         = p.Method,
            Status         = p.Status,            // "Completed" | "Refunded" | "Failed"
            PaidAt         = p.PaymentDate
        };
    }
}
