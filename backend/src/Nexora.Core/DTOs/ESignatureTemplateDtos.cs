using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.DTOs
{
    public class CreateESignatureTemplateRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; }

        [Required]
        public string TemplateUrl { get; set; }

        [MaxLength(50)]
        public string TemplateFormat { get; set; } = "PDF";

        public bool IsActive { get; set; } = true;

        public bool IsPublic { get; set; } = false;

        [MaxLength(50)]
        public string DefaultAuthenticationMethod { get; set; } = "Password";

        public int? DefaultExpiryDays { get; set; }

        [MaxLength(1000)]
        public string DefaultSigningInstructions { get; set; }

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        [MaxLength(20)]
        public string CalendarType { get; set; } = "Gregorian";

        public string Tags { get; set; }

        public List<CreateESignatureTemplateFieldRequest> Fields { get; set; } = new List<CreateESignatureTemplateFieldRequest>();
    }

    public class UpdateESignatureTemplateRequest
    {
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [MaxLength(100)]
        public string Category { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsPublic { get; set; }

        [MaxLength(50)]
        public string DefaultAuthenticationMethod { get; set; }

        public int? DefaultExpiryDays { get; set; }

        [MaxLength(1000)]
        public string DefaultSigningInstructions { get; set; }

        [MaxLength(10)]
        public string Language { get; set; }

        [MaxLength(20)]
        public string CalendarType { get; set; }

        public string Tags { get; set; }
    }

    public class CreateESignatureTemplateFieldRequest
    {
        [Required]
        [MaxLength(100)]
        public string FieldName { get; set; }

        [Required]
        [MaxLength(50)]
        public string FieldType { get; set; } = "Text";

        [MaxLength(200)]
        public string Label { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public bool IsRequired { get; set; } = false;

        public int? PageNumber { get; set; }

        public decimal? PositionX { get; set; }

        public decimal? PositionY { get; set; }

        public decimal? Width { get; set; }

        public decimal? Height { get; set; }

        public string DefaultValue { get; set; }

        public string ValidationRules { get; set; }

        public string Options { get; set; }

        public int DisplayOrder { get; set; }

        [MaxLength(50)]
        public string AssignedRole { get; set; }
    }

    public class UpdateESignatureTemplateFieldRequest
    {
        [MaxLength(100)]
        public string FieldName { get; set; }

        [MaxLength(50)]
        public string FieldType { get; set; }

        [MaxLength(200)]
        public string Label { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public bool? IsRequired { get; set; }

        public int? PageNumber { get; set; }

        public decimal? PositionX { get; set; }

        public decimal? PositionY { get; set; }

        public decimal? Width { get; set; }

        public decimal? Height { get; set; }

        public string DefaultValue { get; set; }

        public string ValidationRules { get; set; }

        public string Options { get; set; }

        public int? DisplayOrder { get; set; }

        [MaxLength(50)]
        public string AssignedRole { get; set; }
    }

    public class ESignatureTemplateResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string TemplateUrl { get; set; }
        public string TemplateHash { get; set; }
        public string TemplateFormat { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublic { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public string FieldDefinitions { get; set; }
        public string SignatureFields { get; set; }
        public string WorkflowConfiguration { get; set; }
        public string DefaultAuthenticationMethod { get; set; }
        public int? DefaultExpiryDays { get; set; }
        public string DefaultSigningInstructions { get; set; }
        public string Language { get; set; }
        public string CalendarType { get; set; }
        public int UsageCount { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public string Version { get; set; }
        public string Tags { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ESignatureTemplateFieldResponse> Fields { get; set; } = new List<ESignatureTemplateFieldResponse>();
        public ESignatureTemplateUsageStats UsageStats { get; set; }
    }

    public class ESignatureTemplateFieldResponse
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string FieldName { get; set; }
        public string FieldType { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public int? PageNumber { get; set; }
        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string DefaultValue { get; set; }
        public string ValidationRules { get; set; }
        public string Options { get; set; }
        public int DisplayOrder { get; set; }
        public string AssignedRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ESignatureTemplateUsageStats
    {
        public int TotalUsage { get; set; }
        public int ActiveDocuments { get; set; }
        public int CompletedDocuments { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal AverageCompletionTime { get; set; }
        public DateTime? LastUsed { get; set; }
        public List<ESignatureTemplateMonthlyUsage> MonthlyUsage { get; set; } = new List<ESignatureTemplateMonthlyUsage>();
    }

    public class ESignatureTemplateMonthlyUsage
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public int UsageCount { get; set; }
        public int CompletedCount { get; set; }
        public decimal CompletionRate { get; set; }
    }

    public class ESignatureTemplateListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Category { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsPublic { get; set; }
        public string SearchTerm { get; set; }
        public string Tags { get; set; }
        public string SortBy { get; set; } = "Name";
        public string SortDirection { get; set; } = "asc";
        public string Language { get; set; }
        public int? CreatedByUserId { get; set; }
    }

    public class ESignatureTemplateListResponse
    {
        public List<ESignatureTemplateResponse> Templates { get; set; } = new List<ESignatureTemplateResponse>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class CreateDocumentFromTemplateRequest
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(1000)]
        public string SigningInstructions { get; set; }

        public bool RequireAllSigners { get; set; } = true;

        public bool AllowDelegation { get; set; } = false;

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; }

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        [MaxLength(20)]
        public string CalendarType { get; set; } = "Gregorian";

        public List<CreateESignatureSignerRequest> Signers { get; set; } = new List<CreateESignatureSignerRequest>();

        public Dictionary<string, string> FieldValues { get; set; } = new Dictionary<string, string>();
    }

    public class ESignatureTemplatePreviewRequest
    {
        [Required]
        public int TemplateId { get; set; }

        public Dictionary<string, string> FieldValues { get; set; } = new Dictionary<string, string>();

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        [MaxLength(20)]
        public string CalendarType { get; set; } = "Gregorian";
    }

    public class ESignatureTemplatePreviewResponse
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string PreviewUrl { get; set; }
        public string PreviewHtml { get; set; }
        public List<ESignatureTemplateFieldPreview> Fields { get; set; } = new List<ESignatureTemplateFieldPreview>();
        public DateTime GeneratedAt { get; set; }
        public string Language { get; set; }
        public string CalendarType { get; set; }
    }

    public class ESignatureTemplateFieldPreview
    {
        public string FieldName { get; set; }
        public string FieldType { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
        public bool IsRequired { get; set; }
        public int? PageNumber { get; set; }
        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string AssignedRole { get; set; }
    }
}
