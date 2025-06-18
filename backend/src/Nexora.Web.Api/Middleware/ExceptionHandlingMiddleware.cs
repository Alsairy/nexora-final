using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nexora.Core.Exceptions;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nexora.Web.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
            
            var errorResponse = CreateErrorResponse(exception, correlationId);
            var statusCode = GetStatusCode(exception);

            LogException(exception, context, correlationId, statusCode);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
            {
                context.Response.Headers.Add("X-Correlation-ID", correlationId);
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private ErrorResponse CreateErrorResponse(Exception exception, string correlationId)
        {
            return exception switch
            {
                ValidationException validationEx => new ErrorResponse
                {
                    Error = "Validation Error",
                    Message = validationEx.Message,
                    ErrorCode = "VALIDATION_FAILED",
                    CorrelationId = correlationId,
                    Details = validationEx.Errors
                },
                UnauthorizedAccessException => new ErrorResponse
                {
                    Error = "Unauthorized",
                    Message = "Access denied. Please check your credentials.",
                    ErrorCode = "UNAUTHORIZED",
                    CorrelationId = correlationId
                },
                NotFoundException notFoundEx => new ErrorResponse
                {
                    Error = "Not Found",
                    Message = notFoundEx.Message,
                    ErrorCode = "NOT_FOUND",
                    CorrelationId = correlationId
                },
                BusinessRuleException businessEx => new ErrorResponse
                {
                    Error = "Business Rule Violation",
                    Message = businessEx.Message,
                    ErrorCode = businessEx.ErrorCode,
                    CorrelationId = correlationId
                },
                TenantNotFoundException tenantEx => new ErrorResponse
                {
                    Error = "Tenant Not Found",
                    Message = tenantEx.Message,
                    ErrorCode = "TENANT_NOT_FOUND",
                    CorrelationId = correlationId
                },
                PaymentException paymentEx => new ErrorResponse
                {
                    Error = "Payment Error",
                    Message = paymentEx.Message,
                    ErrorCode = paymentEx.ErrorCode,
                    CorrelationId = correlationId
                },
                ArgumentException argEx => new ErrorResponse
                {
                    Error = "Invalid Argument",
                    Message = "One or more arguments are invalid.",
                    ErrorCode = "INVALID_ARGUMENT",
                    CorrelationId = correlationId
                },
                InvalidOperationException => new ErrorResponse
                {
                    Error = "Invalid Operation",
                    Message = "The requested operation is not valid in the current state.",
                    ErrorCode = "INVALID_OPERATION",
                    CorrelationId = correlationId
                },
                TimeoutException => new ErrorResponse
                {
                    Error = "Request Timeout",
                    Message = "The request timed out. Please try again later.",
                    ErrorCode = "TIMEOUT",
                    CorrelationId = correlationId
                },
                _ => new ErrorResponse
                {
                    Error = "Internal Server Error",
                    Message = "An unexpected error occurred. Please try again later.",
                    ErrorCode = "INTERNAL_ERROR",
                    CorrelationId = correlationId
                }
            };
        }

        private HttpStatusCode GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ValidationException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                NotFoundException => HttpStatusCode.NotFound,
                BusinessRuleException => HttpStatusCode.BadRequest,
                TenantNotFoundException => HttpStatusCode.NotFound,
                PaymentException paymentEx => paymentEx.ErrorCode switch
                {
                    "INSUFFICIENT_FUNDS" => HttpStatusCode.BadRequest,
                    "PAYMENT_DECLINED" => HttpStatusCode.BadRequest,
                    "PAYMENT_GATEWAY_ERROR" => HttpStatusCode.BadGateway,
                    _ => HttpStatusCode.BadRequest
                },
                ArgumentException => HttpStatusCode.BadRequest,
                InvalidOperationException => HttpStatusCode.BadRequest,
                TimeoutException => HttpStatusCode.RequestTimeout,
                _ => HttpStatusCode.InternalServerError
            };
        }

        private void LogException(Exception exception, HttpContext context, string correlationId, HttpStatusCode statusCode)
        {
            var logLevel = GetLogLevel(statusCode);
            var userId = GetUserId(context);
            var tenantId = GetTenantId(context);

            var logData = new
            {
                CorrelationId = correlationId,
                Exception = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message,
                StatusCode = (int)statusCode,
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                QueryString = context.Request.QueryString.Value,
                UserId = userId,
                TenantId = tenantId,
                UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                RemoteIpAddress = GetClientIpAddress(context),
                Timestamp = DateTimeOffset.UtcNow
            };

            _logger.Log(logLevel, exception, "Unhandled exception occurred: {LogData}", 
                JsonSerializer.Serialize(logData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                }));
        }

        private LogLevel GetLogLevel(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.InternalServerError => LogLevel.Error,
                HttpStatusCode.BadGateway => LogLevel.Error,
                HttpStatusCode.ServiceUnavailable => LogLevel.Error,
                HttpStatusCode.GatewayTimeout => LogLevel.Error,
                HttpStatusCode.Unauthorized => LogLevel.Warning,
                HttpStatusCode.Forbidden => LogLevel.Warning,
                HttpStatusCode.NotFound => LogLevel.Information,
                HttpStatusCode.BadRequest => LogLevel.Information,
                _ => LogLevel.Warning
            };
        }

        private int GetUserId(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(userIdClaim, out var userId) ? userId : 0;
            }
            return 0;
        }

        private int GetTenantId(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var tenantIdClaim = context.User.FindFirst("TenantId")?.Value;
                return int.TryParse(tenantIdClaim, out var tenantId) ? tenantId : 0;
            }
            return 0;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public class ErrorResponse
    {
        public string Error { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public string CorrelationId { get; set; }
        public object Details { get; set; }
    }
}

namespace Nexora.Core.Exceptions
{
    public class ValidationException : Exception
    {
        public object Errors { get; }

        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, object errors) : base(message)
        {
            Errors = errors;
        }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string entityName, object key) 
            : base($"{entityName} with key '{key}' was not found.")
        {
        }
    }

    public class BusinessRuleException : Exception
    {
        public string ErrorCode { get; }

        public BusinessRuleException(string message, string errorCode = "BUSINESS_RULE_VIOLATION") : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    public class TenantNotFoundException : Exception
    {
        public TenantNotFoundException(string message) : base(message)
        {
        }

        public TenantNotFoundException(int tenantId) 
            : base($"Tenant with ID '{tenantId}' was not found.")
        {
        }
    }

    public class PaymentException : Exception
    {
        public string ErrorCode { get; }

        public PaymentException(string message, string errorCode = "PAYMENT_ERROR") : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
