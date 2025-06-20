using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities
{
    [Table("ESignatureAuditLogs")]
    public class ESignatureAuditLog : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        public int? DocumentId { get; set; }

        public int? SignerId { get; set; }

        public int? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } // DocumentCreated, DocumentSent, DocumentViewed, DocumentSigned, etc.

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } // Document, Signer, Workflow, etc.

        [MaxLength(200)]
        public string Location { get; set; }

        public string Metadata { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } // Create, Update, Delete, Sign, View, Send, etc.

        [MaxLength(200)]
        public string EntityName { get; set; }

        [MaxLength(50)]
        public string EntityId { get; set; }

        [Required]
        public string EventData { get; set; } // JSON data containing event details

        public string OldValues { get; set; } // JSON representation of old values

        public string NewValues { get; set; } // JSON representation of new values

        [Required]
        public DateTime EventTimestamp { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        [MaxLength(2000)]
        public string Details { get; set; }

        [MaxLength(45)]
        public string IpAddress { get; set; }

        [MaxLength(500)]
        public string UserAgent { get; set; }

        [MaxLength(200)]
        public string SessionId { get; set; }

        public string GeolocationData { get; set; } // JSON geolocation information

        [MaxLength(100)]
        public string DeviceFingerprint { get; set; }

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; }

        public string AuthenticationReference { get; set; }

        [MaxLength(50)]
        public string EventSeverity { get; set; } // Info, Warning, Error, Critical

        [MaxLength(50)]
        public string EventCategory { get; set; } // Security, Compliance, Business, System

        public bool IsSecurityEvent { get; set; } = false;

        public bool IsComplianceEvent { get; set; } = false;

        [MaxLength(1000)]
        public string Description { get; set; }

        public string AdditionalMetadata { get; set; } // JSON additional metadata

        [MaxLength(100)]
        public string CorrelationId { get; set; } // For tracking related events

        public string LegalEvidenceHash { get; set; } // Hash for legal evidence integrity

        public bool IsAnonymized { get; set; } = false;

        public DateTime? AnonymizedAt { get; set; }

        [MaxLength(50)]
        public string RetentionPolicy { get; set; } // Legal retention requirements

        public DateTime? RetentionExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        [ForeignKey("DocumentId")]
        public virtual ESignatureDocument Document { get; set; }

        [ForeignKey("SignerId")]
        public virtual ESignatureSigner Signer { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
