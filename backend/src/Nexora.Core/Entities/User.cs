using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexora.Core.Entities
{
    [Table("Users")]
    public class User : IEntity, ITenantEntity, ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; }

        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<SmsMessage> SmsMessages { get; set; } = new List<SmsMessage>();
        public virtual ICollection<SmsTemplate> SmsTemplates { get; set; } = new List<SmsTemplate>();
        public virtual ICollection<SenderId> SenderIds { get; set; } = new List<SenderId>();
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public virtual ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
        public virtual ICollection<SmsBilling> SmsBillings { get; set; } = new List<SmsBilling>();
        public virtual ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}
