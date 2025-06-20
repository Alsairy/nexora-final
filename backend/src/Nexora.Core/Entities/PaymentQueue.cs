using System.ComponentModel.DataAnnotations;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities;

public class PaymentQueue : IEntity, ITenantEntity, IAuditableEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    
    [Required]
    public Guid PaymentId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string QueueType { get; set; } = string.Empty; // Processing, Retry, Failed, Completed
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    
    [Required]
    public int Priority { get; set; } = 5; // 1-10, 1 being highest priority
    
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    
    public DateTime ScheduledAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    [MaxLength(2000)]
    public string? ProcessingLog { get; set; }
    
    public string PayloadJson { get; set; } = string.Empty;
    public string? ResponseJson { get; set; }
    
    public virtual Payment Payment { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
}
