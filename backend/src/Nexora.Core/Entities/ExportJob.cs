using System;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class ExportJob : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public string UserId { get; set; }
        public string JobType { get; set; }
        public ExportJobStatus Status { get; set; }
        public string Format { get; set; }
        public string Parameters { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long FileSizeBytes { get; set; }
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public decimal ProgressPercentage { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual User User { get; set; }
    }

    public enum ExportJobStatus
    {
        Queued = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5,
        Expired = 6
    }
}
