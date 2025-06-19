using System;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class RecurringPayment : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string PaymentMethodId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "SAR";
        public RecurringPaymentStatus Status { get; set; }
        public DateTime ScheduledDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string TransactionId { get; set; }
        public string FailureReason { get; set; }
        public int RetryCount { get; set; } = 0;
        public DateTime? NextRetryDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Subscription Subscription { get; set; }
        public virtual PaymentMethod PaymentMethod { get; set; }
        public virtual Transaction Transaction { get; set; }
    }

    public enum RecurringPaymentStatus
    {
        Scheduled = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5,
        Retrying = 6
    }
}
