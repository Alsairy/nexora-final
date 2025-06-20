using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.DTOs
{
    public class CreateESignatureWorkflowRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string WorkflowType { get; set; } = "Sequential";

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; } = "Password";

        public bool RequireAllSteps { get; set; } = true;

        public bool AllowSkipSteps { get; set; } = false;

        public int? ExpiryDays { get; set; }

        public bool SendReminders { get; set; } = true;

        public int? ReminderIntervalHours { get; set; }

        public List<CreateESignatureWorkflowStepRequest> Steps { get; set; } = new List<CreateESignatureWorkflowStepRequest>();
    }

    public class UpdateESignatureWorkflowRequest
    {
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [MaxLength(50)]
        public string WorkflowType { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsDefault { get; set; }

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; }

        public bool? RequireAllSteps { get; set; }

        public bool? AllowSkipSteps { get; set; }

        public int? ExpiryDays { get; set; }

        public bool? SendReminders { get; set; }

        public int? ReminderIntervalHours { get; set; }
    }

    public class CreateESignatureWorkflowStepRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public int StepOrder { get; set; }

        [Required]
        [MaxLength(50)]
        public string StepType { get; set; } = "Sign";

        [Required]
        [MaxLength(50)]
        public string AssigneeType { get; set; } = "Email";

        [Required]
        public string AssigneeValue { get; set; }

        public bool IsRequired { get; set; } = true;

        public bool AllowDelegation { get; set; } = false;

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; }

        public int? TimeoutHours { get; set; }

        public string StepConfiguration { get; set; }
    }

    public class UpdateESignatureWorkflowStepRequest
    {
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public int? StepOrder { get; set; }

        [MaxLength(50)]
        public string StepType { get; set; }

        [MaxLength(50)]
        public string AssigneeType { get; set; }

        public string AssigneeValue { get; set; }

        public bool? IsRequired { get; set; }

        public bool? AllowDelegation { get; set; }

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; }

        public int? TimeoutHours { get; set; }

        public string StepConfiguration { get; set; }
    }

    public class ESignatureWorkflowResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string WorkflowType { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int StepCount { get; set; }
        public string WorkflowDefinition { get; set; }
        public string AuthenticationMethod { get; set; }
        public bool RequireAllSteps { get; set; }
        public bool AllowSkipSteps { get; set; }
        public int? ExpiryDays { get; set; }
        public bool SendReminders { get; set; }
        public int? ReminderIntervalHours { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ESignatureWorkflowStepResponse> Steps { get; set; } = new List<ESignatureWorkflowStepResponse>();
        public ESignatureWorkflowUsageStats UsageStats { get; set; }
    }

    public class ESignatureWorkflowStepResponse
    {
        public int Id { get; set; }
        public int WorkflowId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int StepOrder { get; set; }
        public string StepType { get; set; }
        public string AssigneeType { get; set; }
        public string AssigneeValue { get; set; }
        public bool IsRequired { get; set; }
        public bool AllowDelegation { get; set; }
        public string AuthenticationMethod { get; set; }
        public int? TimeoutHours { get; set; }
        public string StepConfiguration { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ESignatureWorkflowUsageStats
    {
        public int TotalDocuments { get; set; }
        public int CompletedDocuments { get; set; }
        public int ActiveDocuments { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal AverageCompletionTime { get; set; }
        public DateTime? LastUsed { get; set; }
    }

    public class ESignatureWorkflowListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool? IsActive { get; set; }
        public bool? IsDefault { get; set; }
        public string WorkflowType { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; } = "Name";
        public string SortDirection { get; set; } = "asc";
    }

    public class ESignatureWorkflowListResponse
    {
        public List<ESignatureWorkflowResponse> Workflows { get; set; } = new List<ESignatureWorkflowResponse>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class ExecuteWorkflowStepRequest
    {
        [Required]
        public int DocumentId { get; set; }

        [Required]
        public int StepId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } // Complete, Skip, Delegate, Reject

        public string ActionData { get; set; }

        [MaxLength(1000)]
        public string Comments { get; set; }

        public int? DelegatedToUserId { get; set; }

        [MaxLength(1000)]
        public string DelegationReason { get; set; }
    }

    public class WorkflowExecutionResponse
    {
        public int DocumentId { get; set; }
        public int StepId { get; set; }
        public string StepName { get; set; }
        public string Action { get; set; }
        public string Status { get; set; }
        public DateTime ExecutedAt { get; set; }
        public string ExecutedByUserName { get; set; }
        public string Comments { get; set; }
        public string NextStepName { get; set; }
        public int? NextStepId { get; set; }
        public bool IsWorkflowCompleted { get; set; }
    }

    public class WorkflowInstanceResponse
    {
        public int DocumentId { get; set; }
        public string DocumentTitle { get; set; }
        public int WorkflowId { get; set; }
        public string WorkflowName { get; set; }
        public string Status { get; set; }
        public int CurrentStepOrder { get; set; }
        public string CurrentStepName { get; set; }
        public int? CurrentStepId { get; set; }
        public int? NextStepId { get; set; }
        public string NextStepName { get; set; } = string.Empty;
        public int CompletedSteps { get; set; }
        public int TotalSteps { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public decimal ProgressPercentage { get; set; }
        public List<WorkflowStepInstanceResponse> StepInstances { get; set; } = new List<WorkflowStepInstanceResponse>();
    }

    public class WorkflowStepInstanceResponse
    {
        public int StepId { get; set; }
        public string StepName { get; set; }
        public string StepType { get; set; }
        public int StepOrder { get; set; }
        public string Status { get; set; }
        public string AssigneeName { get; set; }
        public string AssigneeEmail { get; set; }
        public string SignerName { get; set; } = string.Empty;
        public int? SignerId { get; set; }
        public int DocumentId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "Normal";
        public string Action { get; set; }
        public string Comments { get; set; }
        public bool IsRequired { get; set; }
        public bool AllowDelegation { get; set; }
        public int? DelegatedToUserId { get; set; }
        public string DelegatedToUserName { get; set; }
        public DateTime? DelegatedAt { get; set; }
        public string DelegationReason { get; set; }
    }
}
