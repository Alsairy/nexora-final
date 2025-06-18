using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class SmsTemplate : IEntity, ITenantEntity, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(1600)]
        public string Content { get; set; }

        [MaxLength(50)]
        public string Category { get; set; }

        [Required]
        [MaxLength(50)]
        public string MessageType { get; set; } = "Transactional";

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Draft";

        [MaxLength(20)]
        public string Language { get; set; } = "en";

        public bool RequiresApproval { get; set; } = false;

        public DateTime? ApprovedAt { get; set; }

        [MaxLength(100)]
        public string ApprovedBy { get; set; }

        [MaxLength(500)]
        public string ApprovalNotes { get; set; }

        public DateTime? RejectedAt { get; set; }

        [MaxLength(100)]
        public string RejectedBy { get; set; }

        [MaxLength(500)]
        public string RejectionReason { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        [MaxLength(500)]
        public string Variables { get; set; }

        public int UsageCount { get; set; } = 0;

        public DateTime? LastUsedAt { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? SuccessRate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? EngagementRate { get; set; }

        public bool IsCompliant { get; set; } = true;

        [MaxLength(1000)]
        public string ComplianceNotes { get; set; }

        public DateTime? ComplianceCheckedAt { get; set; }

        [MaxLength(100)]
        public string ComplianceCheckedBy { get; set; }

        [MaxLength(500)]
        public string Tags { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        [MaxLength(100)]
        public string UpdatedBy { get; set; }

        public virtual Tenant Tenant { get; set; }
        public virtual ICollection<SmsMessage> Messages { get; set; } = new List<SmsMessage>();
    }
}
