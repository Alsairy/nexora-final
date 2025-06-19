using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexora.Core.Entities
{
    [Table("Payments")]
    public class Payment : IEntity, ITenantEntity, ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        public int TransactionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(3)]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency must be a valid 3-letter ISO code")]
        public string Currency { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; }

        [MaxLength(100)]
        public string ExternalReference { get; set; }

        [MaxLength(100)]
        public string IdempotencyKey { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        [ForeignKey("TransactionId")]
        public virtual Transaction Transaction { get; set; }
        
        public virtual PaymentProvider PaymentProvider { get; set; }
        public virtual ICollection<SmsBilling> SmsBillings { get; set; } = new List<SmsBilling>();
    }
}
