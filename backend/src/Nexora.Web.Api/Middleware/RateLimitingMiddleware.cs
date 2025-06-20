using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexora.Core.Interfaces;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nexora.Web.Api.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, 
            IDistributedCache cache, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            if (ShouldSkipRateLimiting(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var rateLimitKey = GetRateLimitKey(context, tenantService);
            var rateLimitConfig = GetRateLimitConfiguration(context);

            var currentCount = await GetCurrentRequestCount(rateLimitKey);
            
            if (currentCount >= rateLimitConfig.MaxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for key {RateLimitKey}. Current count: {CurrentCount}, Limit: {MaxRequests}",
                    rateLimitKey, currentCount, rateLimitConfig.MaxRequests);

                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.Headers.Add("Retry-After", rateLimitConfig.WindowInSeconds.ToString());
                context.Response.Headers.Add("X-RateLimit-Limit", rateLimitConfig.MaxRequests.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", "0");
                context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddSeconds(rateLimitConfig.WindowInSeconds).ToUnixTimeSeconds().ToString());

                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "Rate limit exceeded",
                    message = $"Too many requests. Limit: {rateLimitConfig.MaxRequests} per {rateLimitConfig.WindowInSeconds} seconds",
                    retryAfter = rateLimitConfig.WindowInSeconds
                }));
                return;
            }

            await IncrementRequestCount(rateLimitKey, rateLimitConfig.WindowInSeconds);

            var remaining = Math.Max(0, rateLimitConfig.MaxRequests - currentCount - 1);
            context.Response.Headers.Add("X-RateLimit-Limit", rateLimitConfig.MaxRequests.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", remaining.ToString());
            context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddSeconds(rateLimitConfig.WindowInSeconds).ToUnixTimeSeconds().ToString());

            await _next(context);
        }

        private bool ShouldSkipRateLimiting(PathString path)
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

        private string GetRateLimitKey(HttpContext context, ITenantService tenantService)
        {
            var tenantId = tenantService.GetCurrentTenantId();
            var userId = GetUserId(context.User);
            var ipAddress = GetClientIpAddress(context);
            var endpoint = GetEndpointIdentifier(context);


            if (userId > 0)
            {
                return $"rate_limit:user:{userId}:endpoint:{endpoint}";
            }
            else if (!string.IsNullOrEmpty(tenantId) && int.TryParse(tenantId, out var parsedTenantId) && parsedTenantId > 0)
            {
                return $"rate_limit:tenant:{parsedTenantId}:endpoint:{endpoint}";
            }
            else
            {
                return $"rate_limit:ip:{ipAddress}:endpoint:{endpoint}";
            }
        }

        private RateLimitConfiguration GetRateLimitConfiguration(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            var method = context.Request.Method;
            var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
            var isAdmin = context.User.IsInRole("Admin");

            if (path.StartsWith("/api/auth"))
            {
                return new RateLimitConfiguration
                {
                    MaxRequests = 10, // Stricter for auth endpoints
                    WindowInSeconds = 60
                };
            }
            else if (path.StartsWith("/api/payments") || path.StartsWith("/api/transactions"))
            {
                return new RateLimitConfiguration
                {
                    MaxRequests = isAdmin ? 200 : 50, // Higher limits for admins
                    WindowInSeconds = 60
                };
            }
            else if (method == "POST" || method == "PUT" || method == "DELETE")
            {
                return new RateLimitConfiguration
                {
                    MaxRequests = isAuthenticated ? 100 : 20, // Stricter for write operations
                    WindowInSeconds = 60
                };
            }
            else
            {
                return new RateLimitConfiguration
                {
                    MaxRequests = isAuthenticated ? 1000 : 100, // More lenient for read operations
                    WindowInSeconds = 60
                };
            }
        }

        private async Task<int> GetCurrentRequestCount(string key)
        {
            var countString = await _cache.GetStringAsync(key);
            return int.TryParse(countString, out var count) ? count : 0;
        }

        private async Task IncrementRequestCount(string key, int windowInSeconds)
        {
            var currentCount = await GetCurrentRequestCount(key);
            var newCount = currentCount + 1;

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(windowInSeconds)
            };

            await _cache.SetStringAsync(key, newCount.ToString(), options);
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

        private string GetEndpointIdentifier(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            var method = context.Request.Method;

            var normalizedPath = NormalizePath(path);
            return $"{method}:{normalizedPath}";
        }

        private string NormalizePath(string path)
        {
            return System.Text.RegularExpressions.Regex.Replace(path, @"/\d+", "/{id}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }

    public class RateLimitConfiguration
    {
        public int MaxRequests { get; set; }
        public int WindowInSeconds { get; set; }
    }
}
