using System;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class SmsCompliance : IEntity, ITenantEntity, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string ComplianceType { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        [MaxLength(50)]
        public string Source { get; set; }

        [MaxLength(100)]
        public string Reason { get; set; }

        public DateTime? OptInDate { get; set; }

        public DateTime? OptOutDate { get; set; }

        [MaxLength(50)]
        public string OptInMethod { get; set; }

        [MaxLength(50)]
        public string OptOutMethod { get; set; }

        [MaxLength(500)]
        public string ConsentText { get; set; }

        [MaxLength(100)]
        public string ConsentVersion { get; set; }

        public bool IsGlobalDnd { get; set; } = false;

        public bool IsTenantDnd { get; set; } = false;

        public bool IsTemporaryBlock { get; set; } = false;

        public DateTime? BlockExpiresAt { get; set; }

        [MaxLength(500)]
        public string BlockReason { get; set; }

        public int ViolationCount { get; set; } = 0;

        public DateTime? LastViolationAt { get; set; }

        [MaxLength(500)]
        public string LastViolationReason { get; set; }

        [MaxLength(100)]
        public string PreferredLanguage { get; set; } = "ar";

        [MaxLength(100)]
        public string PreferredTimeZone { get; set; } = "Asia/Riyadh";

        [MaxLength(20)]
        public string AllowedStartTime { get; set; } = "08:00";

        [MaxLength(20)]
        public string AllowedEndTime { get; set; } = "21:00";

        [MaxLength(100)]
        public string AllowedDays { get; set; } = "Sunday,Monday,Tuesday,Wednesday,Thursday";

        public bool AllowPromotional { get; set; } = true;

        public bool AllowTransactional { get; set; } = true;

        public bool AllowOtp { get; set; } = true;

        public bool AllowAlerts { get; set; } = true;

        [MaxLength(500)]
        public string RestrictedCategories { get; set; }

        public int MaxMessagesPerDay { get; set; } = 10;

        public int MaxMessagesPerWeek { get; set; } = 50;

        public int MaxMessagesPerMonth { get; set; } = 200;

        public int MessagesSentToday { get; set; } = 0;

        public int MessagesSentThisWeek { get; set; } = 0;

        public int MessagesSentThisMonth { get; set; } = 0;

        public DateTime? LastMessageSentAt { get; set; }

        [MaxLength(500)]
        public string Metadata { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        [MaxLength(100)]
        public string UpdatedBy { get; set; }

        public virtual Tenant Tenant { get; set; }
    }
}
