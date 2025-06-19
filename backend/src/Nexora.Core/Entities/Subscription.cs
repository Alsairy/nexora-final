using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class Subscription : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "SAR";
        public SubscriptionInterval Interval { get; set; }
        public int IntervalCount { get; set; } = 1;
        public SubscriptionStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public int MaxRetries { get; set; } = 3;
        public int CurrentRetries { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<RecurringPayment> RecurringPayments { get; set; } = new List<RecurringPayment>();
    }

    public enum SubscriptionInterval
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Quarterly = 4,
        Yearly = 5
    }

    public enum SubscriptionStatus
    {
        Active = 1,
        Paused = 2,
        Cancelled = 3,
        Expired = 4,
        PastDue = 5
    }
}
