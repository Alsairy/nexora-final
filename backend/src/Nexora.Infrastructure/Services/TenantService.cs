using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nexora.Core.Configuration;
using Nexora.Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly TenancySettings _settings;
        private readonly ICacheService _cacheService;
        private readonly DbContext _dbContext;
        private int? _currentTenantId;

        public TenantService(
            IHttpContextAccessor httpContextAccessor,
            IOptions<TenancySettings> settings,
            ICacheService cacheService,
            DbContext dbContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _settings = settings.Value;
            _cacheService = cacheService;
            _dbContext = dbContext;
        }

        public int GetCurrentTenantId()
        {
            if (_currentTenantId.HasValue)
            {
                return _currentTenantId.Value;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            
            if (httpContext == null)
            {
                return _settings.DefaultTenantId;
            }

            // Resolve tenant ID based on configuration
            _currentTenantId = _settings.Mode.ToLower() switch
            {
                "subdomain" => ResolveTenantIdFromSubdomain(httpContext),
                "header" => ResolveTenantIdFromHeader(httpContext),
                _ => _settings.DefaultTenantId
            };

            return _currentTenantId.Value;
        }

        public async Task<string> GetCurrentTenantNameAsync()
        {
            var tenantId = GetCurrentTenantId();
            
            return await _cacheService.GetOrCreateAsync(
                $"tenant:{tenantId}:name",
                async () =>
                {
                    var tenant = await _dbContext.Set<Tenant>().FindAsync(tenantId);
                    return tenant?.Name;
                });
        }

        private int ResolveTenantIdFromSubdomain(HttpContext httpContext)
        {
            var host = httpContext.Request.Host.Host;
            
            // Extract subdomain from host
            var subdomain = host.Split('.').FirstOrDefault();
            
            if (string.IsNullOrEmpty(subdomain))
            {
                return _settings.DefaultTenantId;
            }

            // Get tenant ID from cache or database
            var tenantId = _cacheService.GetOrCreateAsync(
                $"subdomain:{subdomain}:tenantId",
                async () =>
                {
                    var tenant = await _dbContext.Set<Tenant>()
                        .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive);
                    
                    return tenant?.Id ?? _settings.DefaultTenantId;
                }).Result;

            return tenantId;
        }

        private int ResolveTenantIdFromHeader(HttpContext httpContext)
        {
            // Get tenant ID from header
            if (!httpContext.Request.Headers.TryGetValue(_settings.HeaderName, out var tenantIdHeader))
            {
                return _settings.DefaultTenantId;
            }

            // Parse tenant ID
            if (!int.TryParse(tenantIdHeader, out var tenantId))
            {
                return _settings.DefaultTenantId;
            }

            // Validate tenant ID
            var isValidTenant = _cacheService.GetOrCreateAsync(
                $"tenant:{tenantId}:exists",
                async () =>
                {
                    return await _dbContext.Set<Tenant>()
                        .AnyAsync(t => t.Id == tenantId && t.IsActive);
                }).Result;

            return isValidTenant ? tenantId : _settings.DefaultTenantId;
        }
    }

    public interface ITenantService
    {
        int GetCurrentTenantId();
        Task<string> GetCurrentTenantNameAsync();
    }
}

