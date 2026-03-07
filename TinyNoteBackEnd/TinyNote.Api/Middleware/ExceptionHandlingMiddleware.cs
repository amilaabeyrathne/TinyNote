using Microsoft.EntityFrameworkCore;
using Npgsql;
using TinyNote.Api.Exceptions;

namespace TinyNote.Api.Middleware
{
    public class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
        {
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            _logger.LogError(exception, "An exception occurred");

            context.Response.StatusCode = exception switch
            {
                DbUpdateException => StatusCodes.Status503ServiceUnavailable,
                NpgsqlException => StatusCodes.Status503ServiceUnavailable,
                ItemNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            await context.Response.WriteAsJsonAsync(new { error = "An error occurred while processing your request." });
        }
    }
}
