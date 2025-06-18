using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Nexora.Web.Api.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            AddSecurityHeaders(context);

            await _next(context);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var response = context.Response;
            var isDevelopment = _configuration.GetValue<bool>("IsDevelopment", false);

            if (!isDevelopment)
            {
                response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            }

            var cspPolicy = BuildContentSecurityPolicy(isDevelopment);
            response.Headers.Add("Content-Security-Policy", cspPolicy);

            response.Headers.Add("X-Content-Type-Options", "nosniff");

            response.Headers.Add("X-Frame-Options", "DENY");

            response.Headers.Add("X-XSS-Protection", "1; mode=block");

            response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

            response.Headers.Add("Permissions-Policy", 
                "camera=(), microphone=(), geolocation=(), payment=(), usb=(), magnetometer=(), gyroscope=(), accelerometer=()");

            response.Headers.Add("Cross-Origin-Embedder-Policy", "require-corp");

            response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin");

            response.Headers.Add("Cross-Origin-Resource-Policy", "same-origin");

            response.Headers.Remove("Server");
            response.Headers.Remove("X-Powered-By");
            response.Headers.Remove("X-AspNet-Version");
            response.Headers.Remove("X-AspNetMvc-Version");

            response.Headers.Add("X-Security-Headers", "enabled");
            response.Headers.Add("X-Content-Security-Policy", cspPolicy);

            if (IsSensitiveEndpoint(context.Request.Path))
            {
                response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, private");
                response.Headers.Add("Pragma", "no-cache");
                response.Headers.Add("Expires", "0");
            }
        }

        private string BuildContentSecurityPolicy(bool isDevelopment)
        {
            var allowedDomains = _configuration.GetSection("Security:AllowedDomains").Get<string[]>() 
                ?? new[] { "https://app.nexora.com", "https://admin.nexora.com" };

            var cspBuilder = new List<string>();

            cspBuilder.Add("default-src 'self'");

            if (isDevelopment)
            {
                cspBuilder.Add("script-src 'self' 'unsafe-inline' 'unsafe-eval' localhost:* 127.0.0.1:*");
            }
            else
            {
                cspBuilder.Add($"script-src 'self' {string.Join(" ", allowedDomains)}");
            }

            if (isDevelopment)
            {
                cspBuilder.Add("style-src 'self' 'unsafe-inline' localhost:* 127.0.0.1:*");
            }
            else
            {
                cspBuilder.Add($"style-src 'self' 'unsafe-inline' {string.Join(" ", allowedDomains)}");
            }

            cspBuilder.Add("img-src 'self' data: https:");

            cspBuilder.Add("font-src 'self' data:");

            if (isDevelopment)
            {
                cspBuilder.Add("connect-src 'self' localhost:* 127.0.0.1:* ws: wss:");
            }
            else
            {
                cspBuilder.Add($"connect-src 'self' {string.Join(" ", allowedDomains)}");
            }

            cspBuilder.Add("media-src 'self'");

            cspBuilder.Add("object-src 'none'");

            cspBuilder.Add("base-uri 'self'");

            cspBuilder.Add("form-action 'self'");

            cspBuilder.Add("frame-ancestors 'none'");

            if (!isDevelopment)
            {
                cspBuilder.Add("upgrade-insecure-requests");
            }

            return string.Join("; ", cspBuilder);
        }

        private bool IsSensitiveEndpoint(PathString path)
        {
            var sensitivePaths = new[]
            {
                "/api/auth",
                "/api/users",
                "/api/payments",
                "/api/transactions",
                "/api/admin"
            };

            return sensitivePaths.Any(sensitivePath => 
                path.StartsWithSegments(sensitivePath, StringComparison.OrdinalIgnoreCase));
        }
    }
}
