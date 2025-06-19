using System;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class ComplianceReport : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public string ReportType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ComplianceReportStatus Status { get; set; }
        public DateTime ReportPeriodStart { get; set; }
        public DateTime ReportPeriodEnd { get; set; }
        public string FilePath { get; set; }
        public string FileFormat { get; set; }
        public long FileSizeBytes { get; set; }
        public string GeneratedBy { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string SubmittedTo { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public enum ComplianceReportStatus
    {
        Draft = 1,
        Generated = 2,
        Submitted = 3,
        Approved = 4,
        Rejected = 5,
        Archived = 6
    }
}
