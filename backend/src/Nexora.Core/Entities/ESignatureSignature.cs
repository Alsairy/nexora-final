using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities
{
    [Table("ESignatureSignatures")]
    public class ESignatureSignature : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        public int DocumentId { get; set; }

        public int SignerId { get; set; }

        [Required]
        [MaxLength(50)]
        public string SignatureType { get; set; } // Electronic, Digital, Biometric, Handwritten

        [Required]
        public string SignatureData { get; set; } // Base64 encoded signature image or certificate data

        public string SignatureHash { get; set; }

        public string CertificateData { get; set; } // Digital certificate information

        public string CertificateFingerprint { get; set; }

        public DateTime SignedAt { get; set; }

        public string IpAddress { get; set; }

        public string UserAgent { get; set; }

        public string GeolocationData { get; set; }

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; }

        public string AuthenticationReference { get; set; }

        public bool IsValid { get; set; } = true;

        public DateTime? ValidatedAt { get; set; }

        public string ValidationReference { get; set; }

        [MaxLength(50)]
        public string SignatureFormat { get; set; } // PNG, SVG, P7S, etc.

        public int? PageNumber { get; set; }

        public decimal? PositionX { get; set; }

        public decimal? PositionY { get; set; }

        public decimal? Width { get; set; }

        public decimal? Height { get; set; }

        public string TimestampData { get; set; } // RFC 3161 timestamp

        public string LegalEvidencePackage { get; set; } // Complete evidence package for legal compliance

        [MaxLength(1000)]
        public string SigningReason { get; set; }

        [MaxLength(200)]
        public string SigningLocation { get; set; }

        [MaxLength(200)]
        public string Location { get; set; }

        public string BiometricData { get; set; } // Encrypted biometric signature data

        public string DeviceInformation { get; set; } // Device fingerprinting data

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

        [ForeignKey("DocumentId")]
        public virtual ESignatureDocument Document { get; set; }

        [ForeignKey("SignerId")]
        public virtual ESignatureSigner Signer { get; set; }
    }
}
