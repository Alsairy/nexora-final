using Nexora.Core.Models;

namespace Nexora.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditEntry auditEntry, CancellationToken cancellationToken = default);
    Task LogAsync(string entityName, string action, string entityId, object? originalValues = null, object? newValues = null, CancellationToken cancellationToken = default);
    Task LogEventAsync(AuditEntry auditEntry, CancellationToken cancellationToken = default);
    Task LogEventAsync(string eventType, string entityName, string entityId, object? oldValues = null, object? newValues = null);
    Task<IEnumerable<AuditEntry>> GetAuditTrailAsync(string entityName, string entityId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEntry>> GetUserAuditTrailAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEntry>> GetTenantAuditTrailAsync(string tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task PurgeOldAuditLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    AuditEntry CreateAuditEntry(string entityName, string action, string entityId, object? originalValues = null, object? newValues = null);
    Task SaveAuditLogs(IEnumerable<AuditEntry> auditEntries, CancellationToken cancellationToken = default);
}
