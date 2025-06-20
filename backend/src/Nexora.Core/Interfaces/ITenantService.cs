namespace Nexora.Core.Interfaces;

public interface ITenantService
{
    Task<string?> GetCurrentTenantIdAsync();
    string? GetCurrentTenantId();
    Task<bool> IsValidTenantAsync(string tenantId);
    Task<string> GetTenantConnectionStringAsync(string tenantId);
    Task<Dictionary<string, object>> GetTenantSettingsAsync(string tenantId);
    Task UpdateTenantSettingsAsync(string tenantId, Dictionary<string, object> settings);
    Task<bool> IsTenantActiveAsync(string tenantId);
    Task<string> GetTenantNameAsync(string tenantId);
    Task<List<string>> GetUserTenantsAsync(string userId);
}
