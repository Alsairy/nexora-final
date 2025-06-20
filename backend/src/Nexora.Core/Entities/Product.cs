using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexora.Core.Entities
{
    [Table("Products")]
    public class Product : IEntity, ITenantEntity, ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        public int MerchantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string ProductType { get; set; }

        [Required]
        [MaxLength(100)]
        public string Sku { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? FixedPrice { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? MinimumPrice { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? MaximumPrice { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; }

        [Required]
        [MaxLength(50)]
        public string SplitType { get; set; }

        [Required]
        public string SplitConfiguration { get; set; }

        [Required]
        [MaxLength(50)]
        public string PayoutFrequency { get; set; }

        public int PayoutDelayDays { get; set; } = 0;

        public bool RequiresKyc { get; set; } = false;
        public bool RequiresInvoice { get; set; } = true;
        public bool AllowPartialPayments { get; set; } = false;
        public bool AllowRefunds { get; set; } = true;

        [MaxLength(1000)]
        public string AllowedPaymentMethods { get; set; }

        [MaxLength(500)]
        public string ProductImageUrl { get; set; }

        [MaxLength(500)]
        public string ProductUrl { get; set; }

        [MaxLength(100)]
        public string Category { get; set; }

        [MaxLength(1000)]
        public string Tags { get; set; }

        [Column(TypeName = "decimal(5,4)")]
        public decimal VatRate { get; set; } = 0.15m;

        public bool IsVatInclusive { get; set; } = true;

        [MaxLength(50)]
        public string VatCategory { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDigital { get; set; } = false;
        public bool RequiresShipping { get; set; } = false;

        [MaxLength(1000)]
        public string ComplianceNotes { get; set; }

        [MaxLength(1000)]
        public string InternalNotes { get; set; }

        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableUntil { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        [ForeignKey("MerchantId")]
        public virtual Merchant Merchant { get; set; }

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<PaymentLink> PaymentLinks { get; set; } = new List<PaymentLink>();
        public virtual ICollection<PaymentSplit> PaymentSplits { get; set; } = new List<PaymentSplit>();
    }
}
