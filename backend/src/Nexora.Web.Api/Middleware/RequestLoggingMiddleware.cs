using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nexora.Core.Interfaces;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nexora.Web.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            if (ShouldSkipLogging(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var correlationId = GetOrCreateCorrelationId(context);
            var stopwatch = Stopwatch.StartNew();
            var requestBody = await CaptureRequestBody(context);

            LogRequest(context, correlationId, tenantService, requestBody);

            var originalResponseBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            Exception exception = null;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                
                var responseBody = await CaptureResponseBody(responseBodyStream);
                
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBodyStream);
                context.Response.Body = originalResponseBodyStream;

                LogResponse(context, correlationId, stopwatch.ElapsedMilliseconds, tenantService, responseBody, exception);
            }
        }

        private bool ShouldSkipLogging(PathString path)
        {
            var skipPaths = new[]
            {
                "/api/health",
                "/swagger",
                "/api/docs",
                "/favicon.ico",
                "/.well-known"
            };

            return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
        }

        private string GetOrCreateCorrelationId(HttpContext context)
        {
            const string correlationIdHeader = "X-Correlation-ID";
            
            if (context.Request.Headers.TryGetValue(correlationIdHeader, out var correlationId))
            {
                return correlationId.FirstOrDefault() ?? Guid.NewGuid().ToString();
            }

            var newCorrelationId = Guid.NewGuid().ToString();
            context.Response.Headers.Add(correlationIdHeader, newCorrelationId);
            return newCorrelationId;
        }

        private async Task<string> CaptureRequestBody(HttpContext context)
        {
            if (!ShouldLogRequestBody(context))
                return null;

            context.Request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(context.Request.ContentLength ?? 0)];
            await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            var requestBody = Encoding.UTF8.GetString(buffer);
            context.Request.Body.Seek(0, SeekOrigin.Begin);

            return SanitizeRequestBody(requestBody, context.Request.Path);
        }

        private async Task<string> CaptureResponseBody(MemoryStream responseBodyStream)
        {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            return responseBody;
        }

        private void LogRequest(HttpContext context, string correlationId, ITenantService tenantService, string requestBody)
        {
            var request = context.Request;
            var user = context.User;

            var logData = new
            {
                CorrelationId = correlationId,
                Timestamp = DateTimeOffset.UtcNow,
                Type = "Request",
                Method = request.Method,
                Path = request.Path.Value,
                QueryString = request.QueryString.Value,
                Headers = GetSafeHeaders(request.Headers),
                UserAgent = request.Headers["User-Agent"].FirstOrDefault(),
                RemoteIpAddress = GetClientIpAddress(context),
                UserId = GetUserId(user),
                UserEmail = GetUserEmail(user),
                TenantId = tenantService.GetCurrentTenantId(),
                ContentType = request.ContentType,
                ContentLength = request.ContentLength,
                RequestBody = requestBody,
                Scheme = request.Scheme,
                Host = request.Host.Value
            };

            _logger.LogInformation("HTTP Request: {LogData}", JsonSerializer.Serialize(logData, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }

        private void LogResponse(HttpContext context, string correlationId, long elapsedMilliseconds, 
            ITenantService tenantService, string responseBody, Exception exception)
        {
            var response = context.Response;
            var user = context.User;

            var logData = new
            {
                CorrelationId = correlationId,
                Timestamp = DateTimeOffset.UtcNow,
                Type = "Response",
                StatusCode = response.StatusCode,
                ElapsedMilliseconds = elapsedMilliseconds,
                ContentType = response.ContentType,
                ContentLength = response.ContentLength,
                Headers = GetSafeHeaders(response.Headers),
                UserId = GetUserId(user),
                UserEmail = GetUserEmail(user),
                TenantId = tenantService.GetCurrentTenantId(),
                ResponseBody = SanitizeResponseBody(responseBody, response.StatusCode),
                Exception = exception?.ToString(),
                Success = exception == null && response.StatusCode < 400
            };

            var logLevel = exception != null ? LogLevel.Error : 
                          response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(logLevel, "HTTP Response: {LogData}", JsonSerializer.Serialize(logData, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }

        private bool ShouldLogRequestBody(HttpContext context)
        {
            var contentType = context.Request.ContentType?.ToLower() ?? "";
            var method = context.Request.Method;
            var path = context.Request.Path.Value?.ToLower() ?? "";

            if (contentType.Contains("multipart/form-data") || 
                contentType.Contains("application/octet-stream") ||
                (context.Request.ContentLength ?? 0) > 10240) // 10KB limit
            {
                return false;
            }

            return method == "POST" || method == "PUT" || method == "PATCH";
        }

        private string SanitizeRequestBody(string requestBody, PathString path)
        {
            if (string.IsNullOrEmpty(requestBody))
                return null;

            try
            {
                var jsonDoc = JsonDocument.Parse(requestBody);
                var sanitized = SanitizeJsonElement(jsonDoc.RootElement, path);
                return JsonSerializer.Serialize(sanitized);
            }
            catch
            {
                return requestBody.Length > 1000 ? requestBody.Substring(0, 1000) + "..." : requestBody;
            }
        }

        private string SanitizeResponseBody(string responseBody, int statusCode)
        {
            if (string.IsNullOrEmpty(responseBody) || statusCode >= 400)
                return responseBody;

            if (responseBody.Length > 2000)
                return responseBody.Substring(0, 2000) + "...";

            return responseBody;
        }

        private object SanitizeJsonElement(JsonElement element, PathString path)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var result = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    if (IsSensitiveField(property.Name))
                    {
                        result[property.Name] = "***REDACTED***";
                    }
                    else
                    {
                        result[property.Name] = SanitizeJsonElement(property.Value, path);
                    }
                }
                return result;
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                return element.EnumerateArray().Select(e => SanitizeJsonElement(e, path)).ToArray();
            }
            else
            {
                return element.GetRawText().Trim('"');
            }
        }

        private bool IsSensitiveField(string fieldName)
        {
            var sensitiveFields = new[]
            {
                "password", "token", "secret", "key", "authorization", "auth",
                "passwordhash", "apikey", "accesstoken", "refreshtoken",
                "creditcard", "cardnumber", "cvv", "ssn", "socialsecurity"
            };

            return sensitiveFields.Any(field => fieldName.ToLower().Contains(field));
        }

        private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
        {
            var safeHeaders = new Dictionary<string, string>();
            var sensitiveHeaders = new[] { "authorization", "cookie", "x-api-key", "x-auth-token" };

            foreach (var header in headers)
            {
                if (sensitiveHeaders.Any(sh => sh.Equals(header.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    safeHeaders[header.Key] = "***REDACTED***";
                }
                else
                {
                    safeHeaders[header.Key] = header.Value.ToString();
                }
            }

            return safeHeaders;
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

        private int GetUserId(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(userIdClaim, out var userId) ? userId : 0;
            }
            return 0;
        }

        private string GetUserEmail(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.FindFirst(ClaimTypes.Email)?.Value ?? "";
            }
            return "";
        }
    }
}
