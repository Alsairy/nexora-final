using Microsoft.AspNetCore.Http;
using Nexora.Core.Interfaces;
using System.Threading.Tasks;

namespace Nexora.Web.Api.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            // Resolve tenant ID
            var tenantId = tenantService.GetCurrentTenantId();
            
            // Add tenant ID to HTTP context items for later use
            context.Items["TenantId"] = tenantId;
            
            // Continue processing
            await _next(context);
        }
    }
}

