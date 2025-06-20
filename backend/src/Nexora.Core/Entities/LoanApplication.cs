using System.ComponentModel.DataAnnotations;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities;

public class LoanApplication : IEntity, ITenantEntity, IAuditableEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ApplicationNumber { get; set; } = string.Empty;
    
    [Required]
    public Guid ApplicantId { get; set; }
    
    [Required]
    public decimal RequestedAmount { get; set; }
    
    [Required]
    public decimal ApprovedAmount { get; set; }
    
    [Required]
    public decimal InterestRate { get; set; }
    
    [Required]
    public int TermMonths { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Disbursed, Closed
    
    [Required]
    [MaxLength(50)]
    public string LoanType { get; set; } = string.Empty; // Personal, Business, Education, etc.
    
    [MaxLength(1000)]
    public string? Purpose { get; set; }
    
    public DateTime ApplicationDate { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public DateTime? DisbursementDate { get; set; }
    public DateTime? MaturityDate { get; set; }
    
    [MaxLength(500)]
    public string? RejectionReason { get; set; }
    
    public decimal MonthlyPayment { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal TotalPayable { get; set; }
    
    public int CreditScore { get; set; }
    public decimal DebtToIncomeRatio { get; set; }
    
    [MaxLength(50)]
    public string RiskCategory { get; set; } = "Medium"; // Low, Medium, High
    
    public virtual User Applicant { get; set; } = null!;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
}
