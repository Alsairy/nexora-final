using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexora.Core.Entities
{
    [Table("PaymentSplits")]
    public class PaymentSplit : IEntity, ITenantEntity, ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        public int PaymentId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int PayoutAccountId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SplitName { get; set; }

        [Required]
        [MaxLength(50)]
        public string SplitType { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? FixedAmount { get; set; }

        [Column(TypeName = "decimal(5,4)")]
        public decimal? PercentageAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal CalculatedAmount { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; }

        [Required]
        [MaxLength(50)]
        public string PayoutFrequency { get; set; }

        public int PayoutDelayDays { get; set; } = 0;

        public DateTime? ScheduledPayoutDate { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? PaidOutAt { get; set; }

        [MaxLength(200)]
        public string PayoutReference { get; set; }

        [MaxLength(500)]
        public string PayoutFailureReason { get; set; }

        public int PayoutRetryCount { get; set; } = 0;
        public DateTime? LastPayoutRetry { get; set; }

        [MaxLength(1000)]
        public string SplitMetadata { get; set; }

        [MaxLength(1000)]
        public string PayoutNotes { get; set; }

        public bool IsManualPayout { get; set; } = false;
        public bool RequiresApproval { get; set; } = false;
        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        [ForeignKey("PaymentId")]
        public virtual Payment Payment { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [ForeignKey("PayoutAccountId")]
        public virtual PayoutAccount PayoutAccount { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual User ApprovedByUser { get; set; }
    }
}
