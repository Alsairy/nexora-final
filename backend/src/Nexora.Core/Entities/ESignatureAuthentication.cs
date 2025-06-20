using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities
{
    [Table("ESignatureAuthentications")]
    public class ESignatureAuthentication : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        public int? DocumentId { get; set; }

        public int? SignerId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AuthenticationType { get; set; } // OTP, Nafath, Password, Certificate

        [Required]
        [MaxLength(50)]
        public string Method { get; set; } // OTP, Nafath, Password, Certificate

        [MaxLength(500)]
        public string Challenge { get; set; }

        [MaxLength(500)]
        public string Response { get; set; }

        [MaxLength(200)]
        public string Location { get; set; }

        public string Metadata { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } // Pending, Initiated, Verified, Failed, Expired

        [MaxLength(200)]
        public string Identifier { get; set; } // Phone number, email, national ID, etc.

        public string AuthenticationData { get; set; } // Encrypted authentication details

        [MaxLength(200)]
        public string TransactionId { get; set; } // External transaction ID (Nafath, OTP provider)

        [MaxLength(100)]
        public string Provider { get; set; } // Nafath, Twilio, AWS SNS, etc.

        public string ProviderResponse { get; set; } // JSON response from provider

        [MaxLength(10)]
        public string Code { get; set; } // OTP code (encrypted)

        public DateTime? CodeGeneratedAt { get; set; }

        public DateTime? CodeExpiresAt { get; set; }

        public int CodeLength { get; set; } = 6;

        public int AttemptCount { get; set; } = 0;

        public int MaxAttempts { get; set; } = 3;

        public DateTime? LastAttemptAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [MaxLength(45)]
        public string IpAddress { get; set; }

        [MaxLength(500)]
        public string UserAgent { get; set; }

        public string GeolocationData { get; set; } // JSON geolocation information

        [MaxLength(100)]
        public string DeviceFingerprint { get; set; }

        [MaxLength(200)]
        public string SessionId { get; set; }

        public string CertificateData { get; set; } // Digital certificate information

        public string CertificateFingerprint { get; set; }

        public string BiometricData { get; set; } // Encrypted biometric authentication data

        [MaxLength(50)]
        public string BiometricType { get; set; } // Fingerprint, FaceID, VoicePrint, etc.

        public string NafathData { get; set; } // JSON Nafath-specific data

        [MaxLength(50)]
        public string NafathTransactionStatus { get; set; } // Nafath transaction status

        public string NafathUserInfo { get; set; } // JSON Nafath user information

        [MaxLength(20)]
        public string NationalId { get; set; } // Saudi national ID for Nafath

        public DateTime? NafathRequestedAt { get; set; }

        public DateTime? NafathRespondedAt { get; set; }

        public DateTime? NafathVerifiedAt { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string FailureReason { get; set; }

        [MaxLength(100)]
        public string FailureCode { get; set; }

        public bool IsTestAuthentication { get; set; } = false;

        public string RiskScore { get; set; } // JSON risk assessment data

        [MaxLength(50)]
        public string RiskLevel { get; set; } // Low, Medium, High, Critical

        public string FraudDetectionData { get; set; } // JSON fraud detection results

        public bool RequiresAdditionalVerification { get; set; } = false;

        [MaxLength(1000)]
        public string AdditionalVerificationReason { get; set; }

        public string ComplianceData { get; set; } // JSON compliance-related data

        public bool IsComplianceVerified { get; set; } = false;

        public DateTime? ComplianceVerifiedAt { get; set; }

        [MaxLength(200)]
        public string ComplianceReference { get; set; }

        public string AuditTrail { get; set; } // JSON detailed audit trail

        [MaxLength(100)]
        public string CorrelationId { get; set; } // For tracking related authentication events

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
    }
}
