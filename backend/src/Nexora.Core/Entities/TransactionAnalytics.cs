using System;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class TransactionAnalytics : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public DateTime Date { get; set; }
        public AnalyticsPeriod Period { get; set; }
        public decimal TotalAmount { get; set; } = 0;
        public int TransactionCount { get; set; } = 0;
        public decimal AverageAmount { get; set; } = 0;
        public decimal MinAmount { get; set; } = 0;
        public decimal MaxAmount { get; set; } = 0;
        public string Currency { get; set; } = "SAR";
        public string TransactionType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
