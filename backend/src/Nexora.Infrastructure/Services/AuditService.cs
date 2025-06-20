using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using Nexora.Core.Data;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly NexoraDbContext _dbContext;
        private readonly ITenantService _tenantService;
        private readonly ICurrentUserService _currentUserService;

        public AuditService(
            NexoraDbContext dbContext,
            ITenantService tenantService,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _tenantService = tenantService;
            _currentUserService = currentUserService;
        }

        public AuditEntry CreateAuditEntry(EntityEntry entry, int? tenantId = null)
        {
            var userId = _currentUserService.GetUserId();
            var tenantIdValue = tenantId?.ToString() ?? _tenantService.GetCurrentTenantId();
            
            var auditEntry = new AuditEntry
            {
                TenantId = tenantIdValue,
                UserId = userId,
                EntityName = entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                Timestamp = DateTime.UtcNow
            };

            // Get entity ID
            var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
            if (idProperty != null)
            {
                auditEntry.EntityId = idProperty.CurrentValue?.ToString();
            }

            // Get changed properties
            foreach (var property in entry.Properties)
            {
                // Skip navigation properties
                if (property.Metadata.IsKey() || property.Metadata.IsForeignKey())
                {
                    continue;
                }

                var propertyName = property.Metadata.Name;
                
                // Handle different states
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;
                    
                    case EntityState.Deleted:
                        auditEntry.OriginalValues[propertyName] = property.OriginalValue;
                        break;
                    
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.OriginalValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }

            return auditEntry;
        }

        public async Task SaveAuditLogs(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
            {
                return;
            }

            // Convert audit entries to audit logs
            var auditLogs = auditEntries.Select(entry => entry.ToAuditLog()).ToList();

            // Save audit logs
            await _dbContext.AuditLogs.AddRangeAsync(auditLogs);
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogEventAsync(string eventType, string entityName, string entityId, object? oldValues = null, object? newValues = null)
        {
            var userId = _currentUserService.GetUserId();
            var auditLog = new AuditLog
            {
                TenantId = int.Parse(_tenantService.GetCurrentTenantId() ?? "1"),
                UserId = userId,
                EntityName = entityName,
                EntityId = entityId,
                Action = eventType,
                OriginalValues = oldValues != null ? JsonConvert.SerializeObject(oldValues) : null,
                NewValues = newValues != null ? JsonConvert.SerializeObject(newValues) : null,
                Timestamp = DateTime.UtcNow
            };

            await _dbContext.AuditLogs.AddAsync(auditLog);
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogEventAsync(AuditEntry auditEntry, CancellationToken cancellationToken = default)
        {
            var auditLog = auditEntry.ToAuditLog();
            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task LogAsync(AuditEntry auditEntry, CancellationToken cancellationToken = default)
        {
            var auditLog = auditEntry.ToAuditLog();
            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task LogAsync(string entityName, string action, string entityId, object? originalValues = null, object? newValues = null, CancellationToken cancellationToken = default)
        {
            await LogEventAsync(action, entityName, entityId, originalValues, newValues);
        }

        public async Task<IEnumerable<AuditEntry>> GetAuditTrailAsync(string entityName, string entityId, CancellationToken cancellationToken = default)
        {
            var auditLogs = await _dbContext.AuditLogs
                .Where(a => a.EntityName == entityName && a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync(cancellationToken);

            return auditLogs.Select(log => new AuditEntry
            {
                TenantId = log.TenantId.ToString(),
                UserId = log.UserId,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Action = log.Action,
                OriginalValues = string.IsNullOrEmpty(log.OriginalValues) ? new Dictionary<string, object?>() : JsonConvert.DeserializeObject<Dictionary<string, object?>>(log.OriginalValues) ?? new Dictionary<string, object?>(),
                NewValues = string.IsNullOrEmpty(log.NewValues) ? new Dictionary<string, object?>() : JsonConvert.DeserializeObject<Dictionary<string, object?>>(log.NewValues) ?? new Dictionary<string, object?>(),
                Timestamp = log.Timestamp
            });
        }

        public async Task<IEnumerable<AuditEntry>> GetUserAuditTrailAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.AuditLogs.AsQueryable();
            
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate.Value);

            var auditLogs = await query.OrderByDescending(a => a.Timestamp).ToListAsync(cancellationToken);

            return auditLogs.Select(log => new AuditEntry
            {
                TenantId = log.TenantId.ToString(),
                UserId = log.UserId,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Action = log.Action,
                OriginalValues = string.IsNullOrEmpty(log.OriginalValues) ? new Dictionary<string, object?>() : JsonConvert.DeserializeObject<Dictionary<string, object?>>(log.OriginalValues) ?? new Dictionary<string, object?>(),
                NewValues = string.IsNullOrEmpty(log.NewValues) ? new Dictionary<string, object?>() : JsonConvert.DeserializeObject<Dictionary<string, object?>>(log.NewValues) ?? new Dictionary<string, object?>(),
                Timestamp = log.Timestamp
            });
        }

        public async Task<IEnumerable<AuditEntry>> GetTenantAuditTrailAsync(string tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.AuditLogs.AsQueryable();
            
            if (int.TryParse(tenantId, out var parsedTenantId))
            {
                query = query.Where(a => a.TenantId == parsedTenantId);
            }

            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate.Value);

            var auditLogs = await query.OrderByDescending(a => a.Timestamp).ToListAsync(cancellationToken);

            return auditLogs.Select(log => new AuditEntry
            {
                TenantId = log.TenantId.ToString(),
                UserId = log.UserId,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Action = log.Action,
                OriginalValues = string.IsNullOrEmpty(log.OriginalValues) ? new Dictionary<string, object?>() : JsonConvert.DeserializeObject<Dictionary<string, object?>>(log.OriginalValues) ?? new Dictionary<string, object?>(),
                NewValues = string.IsNullOrEmpty(log.NewValues) ? new Dictionary<string, object?>() : JsonConvert.DeserializeObject<Dictionary<string, object?>>(log.NewValues) ?? new Dictionary<string, object?>(),
                Timestamp = log.Timestamp
            });
        }

        public async Task PurgeOldAuditLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
        {
            var oldLogs = await _dbContext.AuditLogs
                .Where(a => a.Timestamp < cutoffDate)
                .ToListAsync(cancellationToken);

            _dbContext.AuditLogs.RemoveRange(oldLogs);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public AuditEntry CreateAuditEntry(string entityName, string action, string entityId, object? originalValues = null, object? newValues = null)
        {
            var userId = _currentUserService.GetUserId();
            var tenantId = _tenantService.GetCurrentTenantId();

            return new AuditEntry
            {
                TenantId = tenantId.ToString(),
                UserId = userId,
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                OriginalValues = originalValues as Dictionary<string, object?> ?? new Dictionary<string, object?>(),
                NewValues = newValues as Dictionary<string, object?> ?? new Dictionary<string, object?>(),
                Timestamp = DateTime.UtcNow
            };
        }

        public async Task SaveAuditLogs(IEnumerable<AuditEntry> auditEntries, CancellationToken cancellationToken = default)
        {
            if (auditEntries == null || !auditEntries.Any())
            {
                return;
            }

            var auditLogs = auditEntries.Select(entry => entry.ToAuditLog()).ToList();
            await _dbContext.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

