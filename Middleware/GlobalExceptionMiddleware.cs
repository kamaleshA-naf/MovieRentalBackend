using MovieRentalApp.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace MovieRentalApp.Middleware
{
    [DebuggerNonUserCode]
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            if (context.Response.HasStarted)
                return Task.CompletedTask;

            var statusCode = ex switch
            {
                EntityNotFoundException        => HttpStatusCode.NotFound,
                UnauthorizedException          => HttpStatusCode.Unauthorized,
                UnauthorizedAccessException    => HttpStatusCode.Unauthorized,
                ForbiddenException             => HttpStatusCode.Forbidden,
                BusinessRuleViolationException => HttpStatusCode.Conflict,
                DuplicateEntityException       => HttpStatusCode.Conflict,
                MovieCurrentlyRentedException  => HttpStatusCode.Conflict,
                ArgumentException              => HttpStatusCode.BadRequest,
                KeyNotFoundException           => HttpStatusCode.NotFound,
                _                              => HttpStatusCode.InternalServerError
            };

            var body = JsonSerializer.Serialize(
                new { message = ex.Message },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = (int)statusCode;

            return context.Response.WriteAsync(body);
        }
    }
}
