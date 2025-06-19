using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class PaymentProvider : IEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string ApiEndpoint { get; set; }
        public string ApiVersion { get; set; }
        public string CountryCode { get; set; }
        public string Currency { get; set; }
        public bool IsActive { get; set; }
        public bool SupportsCreditCards { get; set; }
        public bool SupportsDebitCards { get; set; }
        public bool SupportsBankTransfers { get; set; }
        public bool SupportsDigitalWallets { get; set; }
        public bool SupportsRecurringPayments { get; set; }
        public decimal TransactionFeePercentage { get; set; }
        public decimal FixedTransactionFee { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
