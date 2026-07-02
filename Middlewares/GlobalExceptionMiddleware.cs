using System.Text.Json;

namespace InframartAPI_New.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred during the request.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = StatusCodes.Status500InternalServerError;
            var message = "An unexpected error occurred.";
            var errors = new List<string>();

            switch (exception)
            {
                case ValidationException valEx:
                    statusCode = StatusCodes.Status400BadRequest;
                    message = valEx.Message;
                    if (valEx.Errors != null)
                    {
                        errors.AddRange(valEx.Errors);
                    }
                    break;

                case System.ComponentModel.DataAnnotations.ValidationException valEx:
                    statusCode = StatusCodes.Status400BadRequest;
                    message = valEx.Message;
                    break;

                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    message = "Authentication is required to access this resource.";
                    break;

                case ForbiddenException:
                    statusCode = StatusCodes.Status403Forbidden;
                    message = "You do not have permission to access this resource.";
                    break;

                case NotFoundException notFoundEx:
                    statusCode = StatusCodes.Status404NotFound;
                    message = notFoundEx.Message;
                    break;

                case KeyNotFoundException keyEx:
                    statusCode = StatusCodes.Status404NotFound;
                    message = keyEx.Message;
                    break;

                case ConflictException conflictEx:
                    statusCode = StatusCodes.Status409Conflict;
                    message = conflictEx.Message;
                    break;

                case Microsoft.EntityFrameworkCore.DbUpdateException dbEx:
                    statusCode = StatusCodes.Status409Conflict;
                    message = "A database conflict or constraint violation occurred.";
                    break;

                default:
                    // Keep general status code 500 and general message for unhandled errors
                    message = exception.Message;
                    break;
            }

            context.Response.StatusCode = statusCode;

            var responseObj = new
            {
                success = false,
                message = message,
                statusCode = statusCode,
                errors = errors
            };

            var json = JsonSerializer.Serialize(responseObj);
            return context.Response.WriteAsync(json);
        }
    }

    public class ValidationException : Exception
    {
        public List<string>? Errors { get; }
        public ValidationException(string message, List<string>? errors = null) : base(message)
        {
            Errors = errors;
        }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message = "Access forbidden") : base(message) { }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}
