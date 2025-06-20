using System.Security.Claims;

namespace Nexora.Core.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? UserName { get; }
        string? TenantId { get; }
        bool IsAuthenticated { get; }
        ClaimsPrincipal? User { get; }
        
        string GetUserId();
        string GetUserName();
        string GetTenantId();
        bool HasPermission(string permission);
        bool IsInRole(string role);
    }
}
