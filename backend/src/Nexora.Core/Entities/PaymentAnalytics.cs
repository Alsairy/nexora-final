using System;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class PaymentAnalytics : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public DateTime Date { get; set; }
        public AnalyticsPeriod Period { get; set; }
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageTransactionAmount { get; set; }
        public string Currency { get; set; } = "SAR";
        public string PaymentProvider { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public class TransactionAnalytics : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public DateTime Date { get; set; }
        public AnalyticsPeriod Period { get; set; }
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public string Currency { get; set; } = "SAR";
        public string TransactionType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public class RevenueAnalytics : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public DateTime Date { get; set; }
        public AnalyticsPeriod Period { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RecurringRevenue { get; set; }
        public decimal OneTimeRevenue { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal NetRevenue { get; set; }
        public string Currency { get; set; } = "SAR";
        public int NewCustomers { get; set; }
        public int ReturningCustomers { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public enum AnalyticsPeriod
    {
        Hourly = 1,
        Daily = 2,
        Weekly = 3,
        Monthly = 4,
        Quarterly = 5,
        Yearly = 6
    }
}
