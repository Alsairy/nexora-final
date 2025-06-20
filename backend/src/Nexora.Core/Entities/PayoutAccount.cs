using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexora.Core.Entities
{
    [Table("PayoutAccounts")]
    public class PayoutAccount : IEntity, ITenantEntity, ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        public int MerchantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string AccountName { get; set; }

        [Required]
        [MaxLength(200)]
        public string AccountHolderName { get; set; }

        [Required]
        [MaxLength(50)]
        public string AccountType { get; set; }

        [MaxLength(34)]
        public string Iban { get; set; }

        [MaxLength(50)]
        public string VirtualIban { get; set; }

        [MaxLength(20)]
        public string AccountNumber { get; set; }

        [MaxLength(20)]
        public string RoutingNumber { get; set; }

        [MaxLength(20)]
        public string SwiftCode { get; set; }

        [Required]
        [MaxLength(200)]
        public string BankName { get; set; }

        [MaxLength(200)]
        public string BankBranch { get; set; }

        [MaxLength(500)]
        public string BankAddress { get; set; }

        [Required]
        [MaxLength(3)]
        public string Country { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; }

        public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }

        [MaxLength(1000)]
        public string VerificationDocuments { get; set; }

        [MaxLength(1000)]
        public string VerificationNotes { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal MinimumPayoutAmount { get; set; } = 100;

        [Column(TypeName = "decimal(18,4)")]
        public decimal MaximumPayoutAmount { get; set; } = 1000000;

        [Column(TypeName = "decimal(5,4)")]
        public decimal PayoutFeePercentage { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal PayoutFeeFixed { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;

        [MaxLength(1000)]
        public string PayoutSchedule { get; set; }

        [MaxLength(1000)]
        public string ComplianceNotes { get; set; }

        [MaxLength(1000)]
        public string InternalNotes { get; set; }

        public DateTime? LastPayoutDate { get; set; }
        public DateTime? NextScheduledPayout { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalPayoutAmount { get; set; } = 0;

        public int TotalPayoutCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        [ForeignKey("MerchantId")]
        public virtual Merchant Merchant { get; set; }

        public virtual ICollection<PaymentSplit> PaymentSplits { get; set; } = new List<PaymentSplit>();
    }
}
