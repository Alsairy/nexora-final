using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace Nexora.Core.Security;

public interface IComprehensiveAuditService
{
    Task LogUserActionAsync(string action, object data, string userId = null, string ipAddress = null);
    Task LogSecurityEventAsync(SecurityEventType eventType, string description, object data = null, string userId = null);
    Task LogDataAccessAsync(string entityType, string entityId, DataAccessType accessType, string userId = null);
    Task LogSystemEventAsync(string eventType, string description, object data = null);
    Task<IEnumerable<AuditLog>> GetAuditTrailAsync(string entityType, string entityId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<AuditLog>> GetUserAuditTrailAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
}

public class ComprehensiveAuditService : IComprehensiveAuditService
{
    private readonly IRepository<AuditLog> _auditRepository;
    private readonly ILogger<ComprehensiveAuditService> _logger;
    private readonly ITenantService _tenantService;

    public ComprehensiveAuditService(
        IRepository<AuditLog> auditRepository,
        ILogger<ComprehensiveAuditService> logger,
        ITenantService tenantService)
    {
        _auditRepository = auditRepository;
        _logger = logger;
        _tenantService = tenantService;
    }

    public async Task LogUserActionAsync(string action, object data, string userId = null, string ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            EventType = "UserAction",
            Action = action,
            UserId = userId,
            IpAddress = ipAddress,
            Data = JsonSerializer.Serialize(data),
            TenantId = _tenantService.GetCurrentTenantId(),
            Timestamp = DateTime.UtcNow
        };

        await _auditRepository.AddAsync(auditLog);
        await _auditRepository.SaveChangesAsync();

        _logger.LogInformation("User action logged: {Action} by user {UserId} from {IpAddress}", 
            action, userId, ipAddress);
    }

    public async Task LogSecurityEventAsync(SecurityEventType eventType, string description, object data = null, string userId = null)
    {
        var auditLog = new AuditLog
        {
            EventType = "SecurityEvent",
            Action = eventType.ToString(),
            Description = description,
            UserId = userId,
            Data = data != null ? JsonSerializer.Serialize(data) : null,
            TenantId = _tenantService.GetCurrentTenantId(),
            Timestamp = DateTime.UtcNow,
            Severity = GetSeverityForSecurityEvent(eventType)
        };

        await _auditRepository.AddAsync(auditLog);
        await _auditRepository.SaveChangesAsync();

        var logLevel = GetLogLevelForSecurityEvent(eventType);
        _logger.Log(logLevel, "Security event: {EventType} - {Description} for user {UserId}", 
            eventType, description, userId);
    }

    public async Task LogDataAccessAsync(string entityType, string entityId, DataAccessType accessType, string userId = null)
    {
        var auditLog = new AuditLog
        {
            EventType = "DataAccess",
            Action = accessType.ToString(),
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            TenantId = _tenantService.GetCurrentTenantId(),
            Timestamp = DateTime.UtcNow
        };

        await _auditRepository.AddAsync(auditLog);
        await _auditRepository.SaveChangesAsync();

        _logger.LogInformation("Data access logged: {AccessType} on {EntityType}:{EntityId} by user {UserId}", 
            accessType, entityType, entityId, userId);
    }

    public async Task LogSystemEventAsync(string eventType, string description, object data = null)
    {
        var auditLog = new AuditLog
        {
            EventType = "SystemEvent",
            Action = eventType,
            Description = description,
            Data = data != null ? JsonSerializer.Serialize(data) : null,
            TenantId = _tenantService.GetCurrentTenantId(),
            Timestamp = DateTime.UtcNow
        };

        await _auditRepository.AddAsync(auditLog);
        await _auditRepository.SaveChangesAsync();

        _logger.LogInformation("System event logged: {EventType} - {Description}", eventType, description);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditTrailAsync(string entityType, string entityId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = await _auditRepository.GetAsync(a => 
            a.EntityType == entityType && 
            a.EntityId == entityId &&
            (!fromDate.HasValue || a.Timestamp >= fromDate.Value) &&
            (!toDate.HasValue || a.Timestamp <= toDate.Value));

        return query.OrderByDescending(a => a.Timestamp);
    }

    public async Task<IEnumerable<AuditLog>> GetUserAuditTrailAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = await _auditRepository.GetAsync(a => 
            a.UserId == userId &&
            (!fromDate.HasValue || a.Timestamp >= fromDate.Value) &&
            (!toDate.HasValue || a.Timestamp <= toDate.Value));

        return query.OrderByDescending(a => a.Timestamp);
    }

    private string GetSeverityForSecurityEvent(SecurityEventType eventType)
    {
        return eventType switch
        {
            SecurityEventType.LoginSuccess => "Low",
            SecurityEventType.LoginFailure => "Medium",
            SecurityEventType.AccountLocked => "High",
            SecurityEventType.PasswordChanged => "Medium",
            SecurityEventType.UnauthorizedAccess => "High",
            SecurityEventType.SuspiciousActivity => "High",
            SecurityEventType.DataBreach => "Critical",
            SecurityEventType.PrivilegeEscalation => "Critical",
            _ => "Medium"
        };
    }

    private LogLevel GetLogLevelForSecurityEvent(SecurityEventType eventType)
    {
        return eventType switch
        {
            SecurityEventType.LoginSuccess => LogLevel.Information,
            SecurityEventType.LoginFailure => LogLevel.Warning,
            SecurityEventType.AccountLocked => LogLevel.Warning,
            SecurityEventType.PasswordChanged => LogLevel.Information,
            SecurityEventType.UnauthorizedAccess => LogLevel.Error,
            SecurityEventType.SuspiciousActivity => LogLevel.Error,
            SecurityEventType.DataBreach => LogLevel.Critical,
            SecurityEventType.PrivilegeEscalation => LogLevel.Critical,
            _ => LogLevel.Information
        };
    }
}

public enum SecurityEventType
{
    LoginSuccess,
    LoginFailure,
    AccountLocked,
    PasswordChanged,
    UnauthorizedAccess,
    SuspiciousActivity,
    DataBreach,
    PrivilegeEscalation
}

public enum DataAccessType
{
    Read,
    Create,
    Update,
    Delete,
    Export,
    Import
}

public static class AuditLoggingExtensions
{
    public static IServiceCollection AddComprehensiveAuditLogging(this IServiceCollection services)
    {
        services.AddScoped<IComprehensiveAuditService, ComprehensiveAuditService>();
        return services;
    }
}
