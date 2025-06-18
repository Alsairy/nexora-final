using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using Nexora.Core.Data;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
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
            var userId = _currentUserService.GetCurrentUserId();
            var tenantIdValue = tenantId ?? _tenantService.GetCurrentTenantId();
            
            var auditEntry = new AuditEntry
            {
                TenantId = tenantIdValue,
                UserId = userId,
                EntityName = entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                CreatedAt = DateTime.UtcNow
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
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;
                    
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
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
            var auditLogs = auditEntries.Select(entry => new AuditLog
            {
                TenantId = entry.TenantId,
                UserId = entry.UserId,
                EntityName = entry.EntityName,
                EntityId = entry.EntityId,
                Action = entry.Action,
                OldValues = entry.OldValues.Count == 0 ? null : JsonConvert.SerializeObject(entry.OldValues),
                NewValues = entry.NewValues.Count == 0 ? null : JsonConvert.SerializeObject(entry.NewValues),
                CreatedAt = entry.CreatedAt
            }).ToList();

            // Save audit logs
            await _dbContext.AuditLogs.AddRangeAsync(auditLogs);
            await _dbContext.SaveChangesAsync();
        }
    }

    public class AuditEntry
    {
        public int TenantId { get; set; }
        public int? UserId { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
    }

    public interface IAuditService
    {
        AuditEntry CreateAuditEntry(EntityEntry entry, int? tenantId = null);
        Task SaveAuditLogs(List<AuditEntry> auditEntries);
    }
}

