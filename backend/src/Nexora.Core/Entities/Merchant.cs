using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexora.Core.Entities
{
    [Table("Merchants")]
    public class Merchant : IEntity, ITenantEntity, ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string BusinessName { get; set; }

        [Required]
        [MaxLength(200)]
        public string LegalName { get; set; }

        [Required]
        [MaxLength(50)]
        public string BusinessType { get; set; }

        [Required]
        [MaxLength(20)]
        public string CommercialRegistrationNumber { get; set; }

        [Required]
        [MaxLength(15)]
        public string TaxIdentificationNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContactEmail { get; set; }

        [Required]
        [MaxLength(20)]
        public string ContactPhone { get; set; }

        [Required]
        [MaxLength(500)]
        public string BusinessAddress { get; set; }

        [Required]
        [MaxLength(10)]
        public string PostalCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; }

        [Required]
        [MaxLength(3)]
        public string Country { get; set; }

        [Required]
        [MaxLength(50)]
        public string OnboardingStatus { get; set; }

        [Required]
        [MaxLength(50)]
        public string KycStatus { get; set; }

        public DateTime? KycCompletedAt { get; set; }

        [MaxLength(500)]
        public string KycDocuments { get; set; }

        [MaxLength(1000)]
        public string KycNotes { get; set; }

        [Required]
        [MaxLength(34)]
        public string PrimaryIban { get; set; }

        [MaxLength(34)]
        public string SecondaryIban { get; set; }

        [MaxLength(100)]
        public string BankName { get; set; }

        [MaxLength(50)]
        public string BankBranch { get; set; }

        [Required]
        [MaxLength(3)]
        public string SettlementCurrency { get; set; }

        [Required]
        [MaxLength(50)]
        public string SettlementFrequency { get; set; }

        public int SettlementDelayDays { get; set; } = 2;

        [Column(TypeName = "decimal(5,4)")]
        public decimal PlatformFeePercentage { get; set; } = 0.029m;

        [Column(TypeName = "decimal(18,4)")]
        public decimal PlatformFeeFixed { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal MinimumPayoutAmount { get; set; } = 100;

        [Column(TypeName = "decimal(18,4)")]
        public decimal MaximumTransactionAmount { get; set; } = 100000;

        public bool IsActive { get; set; } = true;
        public bool IsVerified { get; set; } = false;
        public bool AllowInternationalPayments { get; set; } = false;
        public bool RequireInvoiceGeneration { get; set; } = true;

        [MaxLength(1000)]
        public string AllowedPaymentMethods { get; set; }

        [MaxLength(500)]
        public string WebhookUrl { get; set; }

        [MaxLength(100)]
        public string WebhookSecret { get; set; }

        [MaxLength(1000)]
        public string BusinessDescription { get; set; }

        [MaxLength(500)]
        public string Website { get; set; }

        [MaxLength(1000)]
        public string ComplianceNotes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<PayoutAccount> PayoutAccounts { get; set; } = new List<PayoutAccount>();
    }
}
