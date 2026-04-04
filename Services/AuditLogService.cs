using MovieRentalApp.Contexts;
using MovieRentalApp.Models;

namespace MovieRentalApp.Services
{
    /// <summary>
    /// Centralized audit log writer.
    /// Never throws — logging must never crash the main business flow.
    /// </summary>
    public class AuditLogService
    {
        private readonly MovieContext _context;

        public AuditLogService(MovieContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Write an audit log entry to the database.
        /// </summary>
        /// <param name="userId"
        /// <param name="userName"
        /// <param name="role">
        /// <param name="message"
        /// <param name="errorNumber"
        public async Task LogAsync(
            int userId,
            string userName,
            string role,
            string message,
            string errorNumber = "")
        {
            try
            {
                // Only write logs for real users (userId > 0)
                if (userId <= 0) return;

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    UserName = userName ?? "Unknown",
                    Role = role ?? "Unknown",
                    Message = message ?? "",
                    ErrorNumber = errorNumber ?? "",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
            catch
            {
                // Swallow — audit log failure must NEVER break the main flow
            }
        }
    }
}