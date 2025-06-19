using System;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class SmsBilling : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public string UserId { get; set; }
        public DateTime BillingPeriodStart { get; set; }
        public DateTime BillingPeriodEnd { get; set; }
        public int MessageCount { get; set; }
        public int DeliveredCount { get; set; }
        public int FailedCount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal CostPerMessage { get; set; }
        public string Currency { get; set; } = "SAR";
        public SmsBillingStatus Status { get; set; }
        public string PaymentId { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? DueDate { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual User User { get; set; }
        public virtual Payment Payment { get; set; }
    }

    public enum SmsBillingStatus
    {
        Pending = 1,
        Paid = 2,
        Overdue = 3,
        Cancelled = 4,
        Refunded = 5
    }
}
