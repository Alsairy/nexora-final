using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nexora.Core.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Nexora.Web.Api.Middleware
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthorizationMiddleware> _logger;

        public AuthorizationMiddleware(RequestDelegate next, ILogger<AuthorizationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            if (ShouldSkipAuthorization(context.Request.Path))
            {
                await _next(context);
                return;
            }

            if (!context.User.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            var userTenantId = GetUserTenantId(context.User);
            var currentTenantId = tenantService.GetCurrentTenantId();

            if (userTenantId != int.Parse(currentTenantId.ToString()))
            {
                _logger.LogWarning("User {UserId} attempted to access tenant {TenantId} but belongs to tenant {UserTenantId}",
                    GetUserId(context.User), currentTenantId, userTenantId);
                
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Forbidden: Invalid tenant access");
                return;
            }

            if (IsAdminEndpoint(context.Request.Path) && !IsUserAdmin(context.User))
            {
                _logger.LogWarning("User {UserId} attempted to access admin endpoint {Path} without admin role",
                    GetUserId(context.User), context.Request.Path);
                
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Forbidden: Admin access required");
                return;
            }

            if (IsExternalApiEndpoint(context.Request.Path))
            {
                var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey) || !await ValidateApiKey(apiKey, tenantService))
                {
                    _logger.LogWarning("Invalid or missing API key for external endpoint {Path}", context.Request.Path);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized: Invalid API key");
                    return;
                }
            }

            if (!await IsAuthorizedForResource(context, tenantService))
            {
                _logger.LogWarning("User {UserId} denied access to resource {Path}",
                    GetUserId(context.User), context.Request.Path);
                
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Forbidden: Insufficient permissions");
                return;
            }

            await _next(context);
        }

        private bool ShouldSkipAuthorization(PathString path)
        {
            var skipPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/health",
                "/swagger",
                "/api/docs",
                "/favicon.ico"
            };

            return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
        }

        private int GetUserTenantId(ClaimsPrincipal user)
        {
            var tenantIdClaim = user.FindFirst("TenantId")?.Value;
            return int.TryParse(tenantIdClaim, out var tenantId) ? tenantId : 0;
        }

        private int GetUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private bool IsUserAdmin(ClaimsPrincipal user)
        {
            return user.IsInRole("Admin");
        }

        private bool IsAdminEndpoint(PathString path)
        {
            var adminPaths = new[]
            {
                "/api/tenants",
                "/api/admin",
                "/api/users"
            };

            return adminPaths.Any(adminPath => path.StartsWithSegments(adminPath, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsExternalApiEndpoint(PathString path)
        {
            var externalApiPaths = new[]
            {
                "/api/external",
                "/api/webhook",
                "/api/integration"
            };

            return externalApiPaths.Any(apiPath => path.StartsWithSegments(apiPath, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<bool> ValidateApiKey(string apiKey, ITenantService tenantService)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 32)
                return false;

            if (!System.Text.RegularExpressions.Regex.IsMatch(apiKey, @"^[a-zA-Z0-9]{32,64}$"))
                return false;


            return true; // Placeholder - implement actual API key validation
        }

        private async Task<bool> IsAuthorizedForResource(HttpContext context, ITenantService tenantService)
        {
            var routeValues = context.Request.RouteValues;
            var httpMethod = context.Request.Method;
            var path = context.Request.Path;

            if (path.StartsWithSegments("/api/users", StringComparison.OrdinalIgnoreCase))
            {
                if (routeValues.TryGetValue("id", out var userIdObj) && int.TryParse(userIdObj?.ToString(), out var resourceUserId))
                {
                    var currentUserId = GetUserId(context.User);
                    var isAdmin = IsUserAdmin(context.User);
                    
                    if (httpMethod == "POST" && !isAdmin)
                        return false;
                    
                    return isAdmin || currentUserId == resourceUserId;
                }
            }

            if (path.StartsWithSegments("/api/transactions", StringComparison.OrdinalIgnoreCase))
            {
                if (httpMethod == "GET" && !routeValues.ContainsKey("id"))
                    return true;

                return true;
            }

            if (path.StartsWithSegments("/api/payments", StringComparison.OrdinalIgnoreCase))
            {
                if (httpMethod == "GET" && !routeValues.ContainsKey("id"))
                    return true;

                return true;
            }

            if (path.StartsWithSegments("/api/tenants", StringComparison.OrdinalIgnoreCase))
            {
                return IsUserAdmin(context.User);
            }

            return true;
        }
    }
}
