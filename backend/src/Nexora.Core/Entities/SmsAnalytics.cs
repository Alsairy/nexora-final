using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexora.Core.Entities
{
    public class SmsAnalytics : IEntity, ITenantEntity, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(50)]
        public string Period { get; set; } = "Daily";

        [MaxLength(100)]
        public string CampaignId { get; set; }

        [MaxLength(100)]
        public string SenderId { get; set; }

        [MaxLength(50)]
        public string MessageType { get; set; }

        [MaxLength(100)]
        public string ProviderId { get; set; }

        [MaxLength(50)]
        public string Country { get; set; } = "SA";

        [MaxLength(50)]
        public string Network { get; set; }

        public int MessagesSent { get; set; } = 0;

        public int MessagesDelivered { get; set; } = 0;

        public int MessagesFailed { get; set; } = 0;

        public int MessagesPending { get; set; } = 0;

        public int MessagesQueued { get; set; } = 0;

        public int MessagesRejected { get; set; } = 0;

        public int MessagesExpired { get; set; } = 0;

        public int MessagesUnknown { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal DeliveryRate { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal FailureRate { get; set; } = 0;

        [Column(TypeName = "decimal(8,2)")]
        public decimal AverageDeliveryTime { get; set; } = 0;

        [Column(TypeName = "decimal(8,2)")]
        public decimal MinDeliveryTime { get; set; } = 0;

        [Column(TypeName = "decimal(8,2)")]
        public decimal MaxDeliveryTime { get; set; } = 0;

        [Column(TypeName = "decimal(12,4)")]
        public decimal TotalCost { get; set; } = 0;

        [Column(TypeName = "decimal(10,6)")]
        public decimal AverageCostPerMessage { get; set; } = 0;

        [MaxLength(10)]
        public string Currency { get; set; } = "SAR";

        public int UniqueRecipients { get; set; } = 0;

        public int RetryAttempts { get; set; } = 0;

        public int ComplianceViolations { get; set; } = 0;

        public int DndBlocks { get; set; } = 0;

        public int TimeWindowBlocks { get; set; } = 0;

        public int ContentFilterBlocks { get; set; } = 0;

        public int RateLimitBlocks { get; set; } = 0;

        public int OptOutRequests { get; set; } = 0;

        public int SpamReports { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal OptOutRate { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal SpamRate { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal EngagementRate { get; set; } = 0;

        public int ClickThroughs { get; set; } = 0;

        public int Conversions { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal ConversionRate { get; set; } = 0;

        [Column(TypeName = "decimal(12,4)")]
        public decimal Revenue { get; set; } = 0;

        [Column(TypeName = "decimal(12,4)")]
        public decimal Roi { get; set; } = 0;

        public int ApiCalls { get; set; } = 0;

        public int ApiErrors { get; set; } = 0;

        [Column(TypeName = "decimal(8,2)")]
        public decimal AverageApiResponseTime { get; set; } = 0;

        [MaxLength(1000)]
        public string TopErrorCodes { get; set; }

        [MaxLength(1000)]
        public string TopFailureReasons { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        [MaxLength(100)]
        public string UpdatedBy { get; set; }

        public virtual Tenant Tenant { get; set; }
    }
}
