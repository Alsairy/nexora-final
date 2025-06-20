using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities
{
    [Table("ESignatureDocuments")]
    public class ESignatureDocument : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } // Draft, Pending, InProgress, Completed, Expired, Cancelled

        [Required]
        [MaxLength(100)]
        public string DocumentType { get; set; }

        [Required]
        public string DocumentUrl { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; }

        public long FileSize { get; set; }

        [Required]
        [MaxLength(100)]
        public string MimeType { get; set; }

        [Required]
        public string DocumentHash { get; set; }

        public long DocumentSize { get; set; }

        [MaxLength(50)]
        public string DocumentFormat { get; set; } // PDF, DOCX, etc.

        public string Metadata { get; set; }

        public int WorkflowId { get; set; }

        public int CreatedByUserId { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(1000)]
        public string SigningInstructions { get; set; }

        public bool RequireAllSigners { get; set; } = true;

        public bool AllowDelegation { get; set; } = false;

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; } // Password, OTP, Nafath, Certificate

        public bool IsTemplate { get; set; } = false;

        public int? TemplateId { get; set; }

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        [MaxLength(20)]
        public string CalendarType { get; set; } = "Gregorian"; // Gregorian, Hijri

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        
        public string? ContentHash { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsArchived { get; set; } = false;
        public DateTime Timestamp { get; set; }
        public string? Content { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        [ForeignKey("WorkflowId")]
        public virtual ESignatureWorkflow Workflow { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; }

        [ForeignKey("TemplateId")]
        public virtual ESignatureTemplate Template { get; set; }

        public virtual ICollection<ESignatureSigner> Signers { get; set; } = new List<ESignatureSigner>();
        public virtual ICollection<ESignatureSignature> Signatures { get; set; } = new List<ESignatureSignature>();
        public virtual ICollection<ESignatureAuditLog> AuditLogs { get; set; } = new List<ESignatureAuditLog>();
        public virtual ICollection<ESignatureNotification> Notifications { get; set; } = new List<ESignatureNotification>();
    }
}
