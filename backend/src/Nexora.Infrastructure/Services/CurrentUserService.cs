using Microsoft.AspNetCore.Http;
using Nexora.Core.Interfaces;
using System.Security.Claims;

namespace Nexora.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
        public string? TenantId => _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenant_id");
        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        public ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public string GetUserId()
        {
            return UserId ?? throw new InvalidOperationException("User ID not found");
        }

        public string GetUserName()
        {
            return UserName ?? throw new InvalidOperationException("User name not found");
        }

        public string GetTenantId()
        {
            return TenantId ?? throw new InvalidOperationException("Tenant ID not found");
        }

        public bool HasPermission(string permission)
        {
            return _httpContextAccessor.HttpContext?.User?.HasClaim("permission", permission) ?? false;
        }

        public bool IsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }
    }
}
