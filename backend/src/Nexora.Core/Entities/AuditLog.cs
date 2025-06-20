using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexora.Core.Entities
{
    [Table("AuditLogs")]
    public class AuditLog : IEntity, ITenantEntity, ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        public string? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; }

        [MaxLength(100)]
        public string? EventType { get; set; }

        public string? Description { get; set; }

        public string? Data { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        public string? Changes { get; set; }

        public string? OriginalValues { get; set; }

        public DateTime Timestamp { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public string? SessionId { get; set; }

        [MaxLength(50)]
        public string? Severity { get; set; }

        [MaxLength(100)]
        public string? EntityType { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant? Tenant { get; set; }
    }
}
