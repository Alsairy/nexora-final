using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexora.Core.Entities
{
    public class SmsMessage : IEntity, ITenantEntity, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(20)]
        public string FromNumber { get; set; }

        [Required]
        [MaxLength(20)]
        public string ToNumber { get; set; }

        [Required]
        [MaxLength(1600)]
        public string MessageContent { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [MaxLength(20)]
        public string SenderId { get; set; }

        [Required]
        [MaxLength(50)]
        public string MessageType { get; set; } = "Transactional";

        public int? TemplateId { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal Cost { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "SAR";

        [MaxLength(100)]
        public string ProviderId { get; set; }

        [MaxLength(100)]
        public string ProviderMessageId { get; set; }

        public DateTime? ScheduledAt { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? FailedAt { get; set; }

        [MaxLength(500)]
        public string ErrorMessage { get; set; }

        [MaxLength(100)]
        public string ErrorCode { get; set; }

        public int RetryCount { get; set; } = 0;

        public int MaxRetries { get; set; } = 3;

        public bool IsCompliant { get; set; } = true;

        [MaxLength(1000)]
        public string ComplianceNotes { get; set; }

        public bool IsDndChecked { get; set; } = false;

        public bool IsTimeWindowChecked { get; set; } = false;

        public bool IsContentFiltered { get; set; } = false;

        [MaxLength(100)]
        public string CampaignId { get; set; }

        [MaxLength(100)]
        public string BatchId { get; set; }

        public int Priority { get; set; } = 1;

        [MaxLength(500)]
        public string Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        [MaxLength(100)]
        public string UpdatedBy { get; set; }

        public virtual Tenant Tenant { get; set; }
        public virtual SmsTemplate Template { get; set; }
        public virtual SmsProvider Provider { get; set; }
    }
}
