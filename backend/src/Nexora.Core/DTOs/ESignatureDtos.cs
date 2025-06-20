using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.DTOs
{
    public class CreateESignatureDocumentRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string DocumentType { get; set; }

        [Required]
        public string DocumentUrl { get; set; }

        public int? WorkflowId { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(1000)]
        public string SigningInstructions { get; set; }

        public bool RequireAllSigners { get; set; } = true;

        public bool AllowDelegation { get; set; } = false;

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; } = "Password";

        public int? TemplateId { get; set; }

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        [MaxLength(20)]
        public string CalendarType { get; set; } = "Gregorian";

        public List<CreateESignatureSignerRequest> Signers { get; set; } = new List<CreateESignatureSignerRequest>();
    }

    public class UpdateESignatureDocumentRequest
    {
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(1000)]
        public string SigningInstructions { get; set; }

        public bool? RequireAllSigners { get; set; }

        public bool? AllowDelegation { get; set; }

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; }

        [MaxLength(10)]
        public string Language { get; set; }

        [MaxLength(20)]
        public string CalendarType { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        [MaxLength(1000)]
        public string AdminNotes { get; set; }
    }

    public class CreateESignatureSignerRequest
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [MaxLength(20)]
        public string NationalId { get; set; }

        [Required]
        [MaxLength(50)]
        public string SignerType { get; set; } = "Primary";

        public int SigningOrder { get; set; } = 1;

        public bool IsRequired { get; set; } = true;

        public bool AllowDelegation { get; set; } = false;

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; }

        [MaxLength(1000)]
        public string SigningInstructions { get; set; }

        public bool EmailNotificationsEnabled { get; set; } = true;

        public bool SmsNotificationsEnabled { get; set; } = false;
    }

    public class UpdateESignatureSignerRequest
    {
        [MaxLength(200)]
        public string FullName { get; set; }

        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [MaxLength(20)]
        public string NationalId { get; set; }

        [MaxLength(50)]
        public string SignerType { get; set; }

        public int? SigningOrder { get; set; }

        public bool? IsRequired { get; set; }

        public bool? AllowDelegation { get; set; }

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; }

        [MaxLength(1000)]
        public string SigningInstructions { get; set; }

        public bool? EmailNotificationsEnabled { get; set; }

        public bool? SmsNotificationsEnabled { get; set; }
    }

    public class CreateSignatureRequest
    {
        [Required]
        public int DocumentId { get; set; }

        [Required]
        public int SignerId { get; set; }

        [Required]
        [MaxLength(50)]
        public string SignatureType { get; set; } = "Electronic";

        [Required]
        public string SignatureData { get; set; }

        public string CertificateData { get; set; }

        [MaxLength(50)]
        public string AuthenticationMethod { get; set; }

        public string AuthenticationReference { get; set; }

        public int? PageNumber { get; set; }

        public decimal? PositionX { get; set; }

        public decimal? PositionY { get; set; }

        public decimal? Width { get; set; }

        public decimal? Height { get; set; }

        [MaxLength(1000)]
        public string SigningReason { get; set; }

        [MaxLength(200)]
        public string SigningLocation { get; set; }

        public string BiometricData { get; set; }

        public string DeviceInformation { get; set; }
    }

    public class ESignatureDocumentResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string DocumentType { get; set; }
        public string DocumentUrl { get; set; }
        public string DocumentHash { get; set; }
        public long DocumentSize { get; set; }
        public string DocumentFormat { get; set; }
        public int? WorkflowId { get; set; }
        public string WorkflowName { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public string CreatedByUserEmail { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string SigningInstructions { get; set; }
        public bool RequireAllSigners { get; set; }
        public bool AllowDelegation { get; set; }
        public string AuthenticationMethod { get; set; }
        public bool IsTemplate { get; set; }
        public int? TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string Language { get; set; }
        public string CalendarType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ESignatureSignerResponse> Signers { get; set; } = new List<ESignatureSignerResponse>();
        public List<ESignatureSignatureResponse> Signatures { get; set; } = new List<ESignatureSignatureResponse>();
        public ESignatureProgressResponse Progress { get; set; }
    }

    public class ESignatureSignerResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string NationalId { get; set; }
        public string SignerType { get; set; }
        public string Status { get; set; }
        public int SigningOrder { get; set; }
        public bool IsRequired { get; set; }
        public bool AllowDelegation { get; set; }
        public string AuthenticationMethod { get; set; }
        public DateTime? InvitedAt { get; set; }
        public DateTime? ViewedAt { get; set; }
        public DateTime? SignedAt { get; set; }
        public DateTime? DeclinedAt { get; set; }
        public string DeclineReason { get; set; }
        public string SigningInstructions { get; set; }
        public bool EmailNotificationsEnabled { get; set; }
        public bool SmsNotificationsEnabled { get; set; }
        public int? DelegatedToUserId { get; set; }
        public string DelegatedToUserName { get; set; }
        public DateTime? DelegatedAt { get; set; }
        public string DelegationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ESignatureSignatureResponse
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int SignerId { get; set; }
        public string SignerName { get; set; }
        public string SignatureType { get; set; }
        public string SignatureData { get; set; }
        public string SignatureHash { get; set; }
        public string CertificateFingerprint { get; set; }
        public DateTime SignedAt { get; set; }
        public string IpAddress { get; set; }
        public string AuthenticationMethod { get; set; }
        public string AuthenticationReference { get; set; }
        public bool IsValid { get; set; }
        public DateTime? ValidatedAt { get; set; }
        public string ValidationReference { get; set; }
        public string SignatureFormat { get; set; }
        public int? PageNumber { get; set; }
        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string SigningReason { get; set; }
        public string SigningLocation { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ESignatureProgressResponse
    {
        public int TotalSigners { get; set; }
        public int SignedCount { get; set; }
        public int PendingCount { get; set; }
        public int DeclinedCount { get; set; }
        public decimal CompletionPercentage { get; set; }
        public string CurrentStep { get; set; }
        public string NextStep { get; set; }
        public DateTime? EstimatedCompletion { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsExpired { get; set; }
        public List<ESignatureStepStatus> Steps { get; set; } = new List<ESignatureStepStatus>();
    }

    public class ESignatureStepStatus
    {
        public int StepOrder { get; set; }
        public string StepName { get; set; }
        public string Status { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string AssigneeName { get; set; }
        public string AssigneeEmail { get; set; }
    }

    public class ESignatureListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Status { get; set; }
        public string DocumentType { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? ExpiryFrom { get; set; }
        public DateTime? ExpiryTo { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
        public bool? IsTemplate { get; set; }
        public int? CreatedByUserId { get; set; }
        public string Language { get; set; }
    }

    public class ESignatureListResponse
    {
        public List<ESignatureDocumentResponse> Documents { get; set; } = new List<ESignatureDocumentResponse>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class SendDocumentForSigningRequest
    {
        [Required]
        public int DocumentId { get; set; }

        [MaxLength(1000)]
        public string Message { get; set; }

        public bool SendImmediately { get; set; } = true;

        public DateTime? ScheduledSendTime { get; set; }

        public List<int> SignerIds { get; set; } = new List<int>();
    }

    public class DeclineSigningRequest
    {
        [Required]
        public int DocumentId { get; set; }

        [Required]
        public int SignerId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; }
    }

    public class DelegateSigningRequest
    {
        [Required]
        public int DocumentId { get; set; }

        [Required]
        public int SignerId { get; set; }

        [Required]
        public int DelegatedToUserId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; }
    }

    public class ESignatureStatisticsResponse
    {
        public int TotalDocuments { get; set; }
        public int CompletedDocuments { get; set; }
        public int PendingDocuments { get; set; }
        public int ExpiredDocuments { get; set; }
        public int CancelledDocuments { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal AverageCompletionTime { get; set; }
        public int TotalSignatures { get; set; }
        public int TotalSigners { get; set; }
        public List<ESignatureMonthlyStats> MonthlyStats { get; set; } = new List<ESignatureMonthlyStats>();
        public List<ESignatureTypeStats> DocumentTypeStats { get; set; } = new List<ESignatureTypeStats>();
    }

    public class ESignatureMonthlyStats
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public int DocumentsCreated { get; set; }
        public int DocumentsCompleted { get; set; }
        public int SignaturesCollected { get; set; }
        public decimal CompletionRate { get; set; }
    }

    public class ESignatureTypeStats
    {
        public string DocumentType { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public decimal AverageCompletionTime { get; set; }
        public decimal CompletionRate { get; set; }
    }
}
