using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Nexora.Core.Interfaces;

namespace Nexora.Core.Entities
{
    [Table("ESignatureWorkflows")]
    public class ESignatureWorkflow : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
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
        [MaxLength(50)]
        public string WorkflowType { get; set; } // Sequential, Parallel, Custom

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } // Active, Inactive, Draft

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        public int StepCount { get; set; }

        public string WorkflowDefinition { get; set; } // JSON configuration

        public string Configuration { get; set; } // JSON configuration



        [MaxLength(50)]
        public string AuthenticationMethod { get; set; } // Password, OTP, Nafath, Certificate

        public bool RequireAllSteps { get; set; } = true;

        public bool AllowSkipSteps { get; set; } = false;

        public int? ExpiryDays { get; set; }

        public bool SendReminders { get; set; } = true;

        public int? ReminderIntervalHours { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User CreatedByUser { get; set; }

        public virtual ICollection<ESignatureDocument> Documents { get; set; } = new List<ESignatureDocument>();
        public virtual ICollection<ESignatureWorkflowStep> Steps { get; set; } = new List<ESignatureWorkflowStep>();
    }

    [Table("ESignatureWorkflowSteps")]
    public class ESignatureWorkflowStep : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        public int WorkflowId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public int StepOrder { get; set; }

        [Required]
        [MaxLength(50)]
        public string StepType { get; set; } // Sign, Review, Approve, Acknowledge

        [Required]
        [MaxLength(50)]
        public string AssigneeType { get; set; } // User, Role, Email

        public string AssigneeValue { get; set; }

        public bool IsRequired { get; set; } = true;

        public bool AllowDelegation { get; set; } = false;

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; }

        public int? TimeoutHours { get; set; }

        public string StepConfiguration { get; set; } // JSON configuration

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        [ForeignKey("WorkflowId")]
        public virtual ESignatureWorkflow Workflow { get; set; }
    }
}
