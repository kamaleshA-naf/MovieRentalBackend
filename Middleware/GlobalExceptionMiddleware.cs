using MovieRentalApp.Contexts;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Middleware;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MovieRentalApp.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context, MovieContext db)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, db, ex);
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            MovieContext db,
            Exception ex)
        {
            var statusCode = ex switch
            {
                EntityNotFoundException => HttpStatusCode.NotFound,
                DuplicateEntityException => HttpStatusCode.Conflict,
                BusinessRuleViolationException => HttpStatusCode.BadRequest,
                UnauthorizedException => HttpStatusCode.Unauthorized,
                UnableToCreateEntityException => HttpStatusCode.InternalServerError,
                _ => HttpStatusCode.InternalServerError
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                try
                {
                    var userIdClaim = context.User
                        .FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var userName = context.User
                        .FindFirst(ClaimTypes.Name)?.Value
                        ?? "Anonymous";
                    var role = context.User
                        .FindFirst(ClaimTypes.Role)?.Value
                        ?? "Anonymous";

                    int.TryParse(userIdClaim, out int userId);

                    if (userId > 0)
                    {
                        db.AuditLogs.Add(new AuditLog
                        {
                            Message = ex.Message,
                            ErrorNumber = ex.HResult.ToString(),
                            Role = role,
                            UserName = userName,
                            UserId = userId,
                            CreatedAt = DateTime.UtcNow
                        });
                        await db.SaveChangesAsync();
                    }
                }
                catch
                {
                    // swallow logging errors
                }
            }

            if (context.Response.HasStarted) return;

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new
                {
                    statusCode = (int)statusCode,      
                    message = ex.Message
                }));
        }
    }
}