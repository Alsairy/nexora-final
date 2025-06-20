using System.ComponentModel.DataAnnotations;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities;

public class PaymentLink : IEntity, ITenantEntity, IAuditableEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "SAR";
    
    [Required]
    [MaxLength(500)]
    public string RedirectUrl { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? CancelUrl { get; set; }
    
    [MaxLength(500)]
    public string? WebhookUrl { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    
    [MaxLength(100)]
    public string? Reference { get; set; }
    
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
}
