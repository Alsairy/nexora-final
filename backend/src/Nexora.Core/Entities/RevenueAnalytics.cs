using System;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class RevenueAnalytics : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public DateTime Date { get; set; }
        public AnalyticsPeriod Period { get; set; }
        public decimal TotalRevenue { get; set; } = 0;
        public decimal RecurringRevenue { get; set; } = 0;
        public decimal OneTimeRevenue { get; set; } = 0;
        public decimal RefundedAmount { get; set; } = 0;
        public decimal NetRevenue { get; set; } = 0;
        public string Currency { get; set; } = "SAR";
        public int NewCustomers { get; set; } = 0;
        public int ReturningCustomers { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
