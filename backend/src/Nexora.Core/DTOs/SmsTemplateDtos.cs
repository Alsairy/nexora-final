using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.DTOs
{
    public class SmsTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
        public string MessageType { get; set; }
        public string Status { get; set; }
        public string Language { get; set; }
        public bool RequiresApproval { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string ApprovedBy { get; set; }
        public string ApprovalNotes { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string RejectedBy { get; set; }
        public string RejectionReason { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public string Variables { get; set; }
        public int UsageCount { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public decimal? SuccessRate { get; set; }
        public decimal? EngagementRate { get; set; }
        public bool IsCompliant { get; set; }
        public string ComplianceNotes { get; set; }
        public DateTime? ComplianceCheckedAt { get; set; }
        public string ComplianceCheckedBy { get; set; }
        public string Tags { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class CreateSmsTemplateRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(1600)]
        public string Content { get; set; }

        [MaxLength(50)]
        public string Category { get; set; }

        [Required]
        [MaxLength(50)]
        public string MessageType { get; set; } = "Transactional";

        [MaxLength(20)]
        public string Language { get; set; } = "en";

        public bool RequiresApproval { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        [MaxLength(500)]
        public string Variables { get; set; }

        [MaxLength(500)]
        public string Tags { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }
    }

    public class UpdateSmsTemplateRequest
    {
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(1600)]
        public string Content { get; set; }

        [MaxLength(50)]
        public string Category { get; set; }

        [MaxLength(50)]
        public string MessageType { get; set; }

        [MaxLength(20)]
        public string Language { get; set; }

        public bool? RequiresApproval { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsDefault { get; set; }

        [MaxLength(500)]
        public string Variables { get; set; }

        [MaxLength(500)]
        public string Tags { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }
    }

    public class SmsTemplateApprovalRequest
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } // "Approve" or "Reject"

        [MaxLength(500)]
        public string Notes { get; set; }
    }

    public class SmsTemplateListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string Status { get; set; }
        public string MessageType { get; set; }
        public string Category { get; set; }
        public string Language { get; set; }
        public bool? RequiresApproval { get; set; }
        public bool? IsActive { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; } = "CreatedAt";
        public string SortOrder { get; set; } = "DESC";
    }

    public class SmsTemplateListResponse
    {
        public List<SmsTemplateDto> Templates { get; set; } = new List<SmsTemplateDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class SmsTemplateVariableDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public string DefaultValue { get; set; }
        public string ValidationPattern { get; set; }
    }

    public class SmsTemplatePreviewRequest
    {
        [Required]
        public int TemplateId { get; set; }

        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    }

    public class SmsTemplatePreviewResponse
    {
        public string PreviewContent { get; set; }
        public int CharacterCount { get; set; }
        public int SmsSegments { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Currency { get; set; } = "SAR";
        public List<string> MissingVariables { get; set; } = new List<string>();
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public bool IsCompliant { get; set; }
        public List<string> ComplianceWarnings { get; set; } = new List<string>();
    }

    public class SmsTemplateStatsDto
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public int TotalUsage { get; set; }
        public int SuccessfulSends { get; set; }
        public int FailedSends { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageDeliveryTime { get; set; }
        public decimal TotalCost { get; set; }
        public string Currency { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public Dictionary<string, int> UsageByMessageType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> UsageByProvider { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, decimal> CostByProvider { get; set; } = new Dictionary<string, decimal>();
    }

    public class SmsTemplateComplianceCheckRequest
    {
        [Required]
        public int TemplateId { get; set; }

        [MaxLength(50)]
        public string MessageType { get; set; }

        [MaxLength(20)]
        public string SenderId { get; set; }
    }

    public class SmsTemplateComplianceCheckResponse
    {
        public bool IsCompliant { get; set; }
        public List<string> Violations { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public bool RequiresSenderIdApproval { get; set; }
        public bool RequiresContentApproval { get; set; }
        public bool HasProhibitedContent { get; set; }
        public bool HasSuspiciousUrls { get; set; }
        public List<string> DetectedKeywords { get; set; } = new List<string>();
        public string ComplianceScore { get; set; }
        public DateTime CheckedAt { get; set; }
    }
}
