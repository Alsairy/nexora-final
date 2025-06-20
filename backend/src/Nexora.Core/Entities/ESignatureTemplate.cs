using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities
{
    [Table("ESignatureTemplates")]
    public class ESignatureTemplate : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; }

        [Required]
        public string TemplateUrl { get; set; }

        public string TemplateHash { get; set; }

        [MaxLength(50)]
        public string TemplateFormat { get; set; } // PDF, DOCX, etc.

        public bool IsActive { get; set; } = true;

        public bool IsPublic { get; set; } = false;

        public int CreatedByUserId { get; set; }

        public string FieldDefinitions { get; set; } // JSON configuration for form fields

        public string SignatureFields { get; set; } // JSON configuration for signature placement

        public string WorkflowConfiguration { get; set; } // JSON workflow configuration

        public string TemplateData { get; set; } // JSON template data



        [MaxLength(50)]
        public string DefaultAuthenticationMethod { get; set; }

        public int? DefaultExpiryDays { get; set; }

        [MaxLength(1000)]
        public string DefaultSigningInstructions { get; set; }

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        [MaxLength(20)]
        public string CalendarType { get; set; } = "Gregorian";

        public int UsageCount { get; set; } = 0;

        public DateTime? LastUsedAt { get; set; }

        public int Version { get; set; } = 1;

        [MaxLength(50)]
        public string VersionString { get; set; } = "1.0";

        public string Tags { get; set; } // Comma-separated tags for categorization

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; }

        public virtual ICollection<ESignatureDocument> Documents { get; set; } = new List<ESignatureDocument>();
        public virtual ICollection<ESignatureTemplateField> Fields { get; set; } = new List<ESignatureTemplateField>();
    }
}
