using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class SenderId : IEntity, ITenantEntity, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(20)]
        public string SenderIdValue { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "Alphanumeric";

        [MaxLength(100)]
        public string Purpose { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [MaxLength(100)]
        public string CompanyName { get; set; }

        [MaxLength(100)]
        public string ContactPerson { get; set; }

        [MaxLength(20)]
        public string ContactPhone { get; set; }

        [MaxLength(100)]
        public string ContactEmail { get; set; }

        [MaxLength(500)]
        public string BusinessLicense { get; set; }

        [MaxLength(500)]
        public string SampleMessages { get; set; }

        public DateTime? SubmittedAt { get; set; }

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

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; } = false;

        public bool IsDefault { get; set; } = false;

        [MaxLength(100)]
        public string CitcRegistrationId { get; set; }

        public DateTime? CitcSubmittedAt { get; set; }

        public DateTime? CitcApprovedAt { get; set; }

        [MaxLength(500)]
        public string CitcNotes { get; set; }

        [MaxLength(100)]
        public string StcStatus { get; set; }

        [MaxLength(100)]
        public string MobilyStatus { get; set; }

        [MaxLength(100)]
        public string ZainStatus { get; set; }

        [MaxLength(100)]
        public string VirginStatus { get; set; }

        public DateTime? StcApprovedAt { get; set; }

        public DateTime? MobilyApprovedAt { get; set; }

        public DateTime? ZainApprovedAt { get; set; }

        public DateTime? VirginApprovedAt { get; set; }

        public int UsageCount { get; set; } = 0;

        public DateTime? LastUsedAt { get; set; }

        [MaxLength(500)]
        public string RestrictedKeywords { get; set; }

        [MaxLength(500)]
        public string AllowedMessageTypes { get; set; }

        public int? ProviderId { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        [MaxLength(100)]
        public string UpdatedBy { get; set; }

        public virtual Tenant Tenant { get; set; }
        public virtual SmsProvider Provider { get; set; }
        public virtual ICollection<SmsMessage> Messages { get; set; } = new List<SmsMessage>();
    }
}
