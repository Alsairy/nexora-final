using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class PaymentMethod : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public string UserId { get; set; }
        public PaymentMethodType Type { get; set; }
        public string Provider { get; set; }
        public string ExternalId { get; set; }
        public string LastFourDigits { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public string CardholderName { get; set; }
        public string BankName { get; set; }
        public string AccountType { get; set; }
        public bool IsDefault { get; set; }
        public bool IsVerified { get; set; }
        public PaymentMethodStatus Status { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<RecurringPayment> RecurringPayments { get; set; } = new List<RecurringPayment>();
    }

    public enum PaymentMethodType
    {
        CreditCard = 1,
        DebitCard = 2,
        BankAccount = 3,
        DigitalWallet = 4,
        Mada = 5,
        ApplePay = 6,
        GooglePay = 7,
        SamsungPay = 8
    }

    public enum PaymentMethodStatus
    {
        Active = 1,
        Inactive = 2,
        Expired = 3,
        Blocked = 4,
        PendingVerification = 5
    }
}
