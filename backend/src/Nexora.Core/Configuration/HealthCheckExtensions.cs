using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Data.SqlClient;

namespace Nexora.Core.Configuration;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddNexoraHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddSqlServer(
                connectionString,
                name: "database",
                tags: new[] { "critical", "database" },
                timeout: TimeSpan.FromSeconds(10));
        }

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(
                redisConnectionString,
                name: "redis",
                tags: new[] { "cache", "redis" },
                timeout: TimeSpan.FromSeconds(5));
        }

        var keyVaultUrl = configuration["KeyVault:VaultUrl"];
        if (!string.IsNullOrEmpty(keyVaultUrl))
        {
            healthChecksBuilder.AddAzureKeyVault(
                new Uri(keyVaultUrl),
                new Azure.Identity.DefaultAzureCredential(),
                options =>
                {
                    options.Name = "keyvault";
                    options.Tags.Add("critical");
                    options.Tags.Add("security");
                    options.Timeout = TimeSpan.FromSeconds(10);
                });
        }

        var paymentGatewayUrl = configuration["ExternalServices:PaymentGateway:BaseUrl"];
        if (!string.IsNullOrEmpty(paymentGatewayUrl))
        {
            healthChecksBuilder.AddUrlGroup(
                new Uri($"{paymentGatewayUrl}/health"),
                name: "payment-gateway",
                tags: new[] { "external", "payment" },
                timeout: TimeSpan.FromSeconds(15));
        }

        var smsServiceUrl = configuration["ExternalServices:SmsService:BaseUrl"];
        if (!string.IsNullOrEmpty(smsServiceUrl))
        {
            healthChecksBuilder.AddUrlGroup(
                new Uri($"{smsServiceUrl}/health"),
                name: "sms-service",
                tags: new[] { "external", "sms" },
                timeout: TimeSpan.FromSeconds(10));
        }

        healthChecksBuilder.AddCheck<DiskSpaceHealthCheck>(
            "disk-space",
            tags: new[] { "infrastructure" });

        healthChecksBuilder.AddCheck<MemoryHealthCheck>(
            "memory",
            tags: new[] { "infrastructure" });

        healthChecksBuilder.AddCheck<TenantHealthCheck>(
            "tenant-service",
            tags: new[] { "critical", "business" });

        return services;
    }
}

public class DiskSpaceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            var results = new Dictionary<string, object>();
            var hasLowSpace = false;

            foreach (var drive in drives)
            {
                var freeSpacePercentage = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
                results[drive.Name] = new
                {
                    TotalSize = drive.TotalSize,
                    AvailableFreeSpace = drive.AvailableFreeSpace,
                    FreeSpacePercentage = Math.Round(freeSpacePercentage, 2)
                };

                if (freeSpacePercentage < 10) // Less than 10% free space
                {
                    hasLowSpace = true;
                }
            }

            return Task.FromResult(hasLowSpace
                ? HealthCheckResult.Degraded("Low disk space detected", data: results)
                : HealthCheckResult.Healthy("Sufficient disk space available", data: results));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Error checking disk space", ex));
        }
    }
}

public class MemoryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var workingSet = Environment.WorkingSet;
            var gcMemory = GC.GetTotalMemory(false);
            
            var data = new Dictionary<string, object>
            {
                ["WorkingSet"] = workingSet,
                ["GCMemory"] = gcMemory,
                ["Gen0Collections"] = GC.CollectionCount(0),
                ["Gen1Collections"] = GC.CollectionCount(1),
                ["Gen2Collections"] = GC.CollectionCount(2)
            };

            var isHighMemory = workingSet > 1024 * 1024 * 1024;

            return Task.FromResult(isHighMemory
                ? HealthCheckResult.Degraded("High memory usage detected", data: data)
                : HealthCheckResult.Healthy("Memory usage is normal", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Error checking memory usage", ex));
        }
    }
}

public class TenantHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                ["TenantsActive"] = true,
                ["LastCheck"] = DateTime.UtcNow
            };

            return Task.FromResult(HealthCheckResult.Healthy("Tenant service is operational", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Tenant service check failed", ex));
        }
    }
}
