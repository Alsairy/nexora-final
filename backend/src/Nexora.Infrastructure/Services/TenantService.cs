using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nexora.Core.Configuration;
using Nexora.Core.Interfaces;
using Nexora.Core.Entities;
using Nexora.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly TenancySettings _settings;
        private readonly ICacheService _cacheService;
        private readonly NexoraDbContext _dbContext;
        private int? _currentTenantId;

        public TenantService(
            IHttpContextAccessor httpContextAccessor,
            IOptions<TenancySettings> settings,
            ICacheService cacheService,
            NexoraDbContext dbContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _settings = settings?.Value ?? new TenancySettings 
            { 
                Mode = "Header", 
                HeaderName = "X-Tenant-ID", 
                DefaultTenantId = 1,
                EnableMultiTenancy = true,
                TenantResolutionStrategy = "Header"
            };
            _cacheService = cacheService;
            _dbContext = dbContext;
        }

        public string? GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            if (httpContext == null || _settings == null)
            {
                return "1"; // Default tenant ID as string
            }

            // Resolve tenant ID based on configuration
            var tenantId = (_settings.Mode?.ToLower()) switch
            {
                "subdomain" => ResolveTenantIdFromSubdomain(httpContext),
                "header" => ResolveTenantIdFromHeader(httpContext),
                _ => _settings.DefaultTenantId
            };

            return tenantId.ToString();
        }

        public async Task<string?> GetCurrentTenantIdAsync()
        {
            return GetCurrentTenantId();
        }

        public async Task<bool> IsValidTenantAsync(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
                return false;

            if (!int.TryParse(tenantId, out var id))
                return false;

            return await _dbContext.Set<Tenant>().AnyAsync(t => t.Id == id && t.IsActive);
        }

        public async Task<string> GetTenantConnectionStringAsync(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId) || !int.TryParse(tenantId, out var id))
                return "DefaultConnection";

            var tenant = await _dbContext.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == id);
            return tenant?.ConnectionString ?? "DefaultConnection";
        }

        public async Task<Dictionary<string, object>> GetTenantSettingsAsync(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId) || !int.TryParse(tenantId, out var id))
                return new Dictionary<string, object>();

            var tenant = await _dbContext.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == id);
            return tenant?.Settings ?? new Dictionary<string, object>();
        }

        public async Task UpdateTenantSettingsAsync(string tenantId, Dictionary<string, object> settings)
        {
            if (string.IsNullOrEmpty(tenantId) || !int.TryParse(tenantId, out var id))
                return;

            var tenant = await _dbContext.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == id);
            if (tenant != null)
            {
                tenant.Settings = settings;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> IsTenantActiveAsync(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId) || !int.TryParse(tenantId, out var id))
                return false;

            var tenant = await _dbContext.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == id);
            return tenant?.IsActive ?? false;
        }

        public async Task<string> GetTenantNameAsync(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId) || !int.TryParse(tenantId, out var id))
                return "Default";

            return await _cacheService.GetOrCreateAsync(
                $"tenant:{id}:name",
                async () =>
                {
                    var tenant = await _dbContext.Set<Tenant>().FindAsync(id);
                    return tenant?.Name ?? "Unknown";
                });
        }

        public async Task<List<string>> GetUserTenantsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
                return new List<string>();

            var userTenants = await _dbContext.Set<User>()
                .Where(u => u.Id == id)
                .Select(u => u.TenantId.ToString())
                .ToListAsync();

            return userTenants;
        }

        private int ResolveTenantIdFromSubdomain(HttpContext httpContext)
        {
            try
            {
                var host = httpContext.Request.Host.Host;
                
                // Extract subdomain from host
                var subdomain = host.Split('.').FirstOrDefault();
                
                if (string.IsNullOrEmpty(subdomain))
                {
                    return _settings?.DefaultTenantId ?? 1;
                }

                return _settings?.DefaultTenantId ?? 1;
            }
            catch
            {
                return _settings?.DefaultTenantId ?? 1;
            }
        }

        private int ResolveTenantIdFromHeader(HttpContext httpContext)
        {
            try
            {
                var headerName = _settings?.HeaderName ?? "X-Tenant-ID";
                
                // Get tenant ID from header
                if (!httpContext.Request.Headers.TryGetValue(headerName, out var tenantIdHeader))
                {
                    return _settings?.DefaultTenantId ?? 1;
                }

                // Parse tenant ID
                if (!int.TryParse(tenantIdHeader, out var tenantId))
                {
                    return _settings?.DefaultTenantId ?? 1;
                }

                return tenantId > 0 ? tenantId : (_settings?.DefaultTenantId ?? 1);
            }
            catch
            {
                return _settings?.DefaultTenantId ?? 1;
            }
        }
    }


}

