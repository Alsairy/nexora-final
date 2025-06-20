using System.ComponentModel.DataAnnotations;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities;

public class ZatcaInvoice : IEntity, ITenantEntity, IAuditableEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty;
    
    [Required]
    public DateTime IssueDate { get; set; }
    
    [Required]
    public decimal TotalAmount { get; set; }
    
    [Required]
    public decimal VatAmount { get; set; }
    
    [Required]
    public decimal NetAmount { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "SAR";
    
    [Required]
    [MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(15)]
    public string SupplierVatNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;
    
    [MaxLength(15)]
    public string? CustomerVatNumber { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Rejected
    
    [MaxLength(500)]
    public string? ZatcaResponse { get; set; }
    
    [MaxLength(100)]
    public string? ZatcaInvoiceHash { get; set; }
    
    [MaxLength(100)]
    public string? ZatcaUuid { get; set; }
    
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public string InvoiceXml { get; set; } = string.Empty;
    public string? QrCode { get; set; }
    
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
}
