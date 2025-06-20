using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities
{
    [Table("ESignatureSigners")]
    public class ESignatureSigner : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        public int DocumentId { get; set; }

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(200)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [MaxLength(20)]
        public string NationalId { get; set; }

        [Required]
        [MaxLength(50)]
        public string SignerType { get; set; } // Primary, Secondary, Witness, Approver

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } // Signer, Reviewer, Approver

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        [MaxLength(20)]
        public string CalendarType { get; set; } = "Gregorian";

        public int? UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } // Pending, Invited, Viewed, Signed, Declined, Expired

        public int SigningOrder { get; set; }

        public bool IsRequired { get; set; } = true;

        public bool AllowDelegation { get; set; } = false;

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; } // Password, OTP, Nafath, Certificate

        public string AuthenticationData { get; set; } // Encrypted authentication details

        public DateTime? InvitedAt { get; set; }

        public DateTime? ViewedAt { get; set; }

        public DateTime? SignedAt { get; set; }

        public DateTime? DeclinedAt { get; set; }

        [MaxLength(1000)]
        public string DeclineReason { get; set; }

        public string SigningToken { get; set; }

        public DateTime? TokenExpiryDate { get; set; }

        public string IpAddress { get; set; }

        public string UserAgent { get; set; }

        public string GeolocationData { get; set; }

        [MaxLength(1000)]
        public string SigningInstructions { get; set; }

        public bool EmailNotificationsEnabled { get; set; } = true;

        public bool SmsNotificationsEnabled { get; set; } = false;

        public int? DelegatedToUserId { get; set; }

        public DateTime? DelegatedAt { get; set; }

        [MaxLength(1000)]
        public string DelegationReason { get; set; }
        
        public DateTime? LastReminderSentAt { get; set; }
        public int ReminderCount { get; set; } = 0;
        
        [NotMapped]
        public string Name => FullName;
        
        [NotMapped]
        public string FirstName => FullName?.Split(' ').FirstOrDefault() ?? "";
        
        [NotMapped]
        public string LastName => FullName?.Contains(' ') == true ? FullName.Substring(FullName.IndexOf(' ') + 1) : "";

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

        [ForeignKey("DelegatedToUserId")]
        public virtual User DelegatedToUser { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public virtual ICollection<ESignatureSignature> Signatures { get; set; } = new List<ESignatureSignature>();
        public virtual ICollection<ESignatureAuthentication> Authentications { get; set; } = new List<ESignatureAuthentication>();
        public virtual ICollection<ESignatureNotification> Notifications { get; set; } = new List<ESignatureNotification>();
    }
}
