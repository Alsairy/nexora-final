using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities
{
    [Table("NotificationLogs")]
    public class NotificationLog : IEntity, ITenantEntity, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        public int? UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string NotificationType { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Recipient { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [MaxLength(50)]
        public string Channel { get; set; } = "Email";

        [MaxLength(50)]
        public string Priority { get; set; } = "Normal";

        public DateTime? SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? ClickedAt { get; set; }

        [MaxLength(1000)]
        public string ErrorMessage { get; set; } = string.Empty;

        public int RetryCount { get; set; } = 0;

        public string Metadata { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
