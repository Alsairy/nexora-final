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

        public int? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; }

        public string OldValues { get; set; }

        public string NewValues { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
