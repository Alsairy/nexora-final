using System;
using System.Collections.Generic;

namespace Nexora.Core.DTOs;

public class ComplianceValidationResult
{
    public bool IsCompliant { get; set; }
    public bool IsValid { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Violations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> ValidationDetails { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
    public string ValidatedBy { get; set; } = string.Empty;
    public int TenantId { get; set; }
}

public class LegalEvidencePackage
{
    public string Id { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public List<EvidenceItem> EvidenceItems { get; set; } = new List<EvidenceItem>();
    public List<SignatureEvidence> Signatures { get; set; } = new List<SignatureEvidence>();
    public List<AuditTrailEntry> AuditTrail { get; set; } = new List<AuditTrailEntry>();
    public string PackageType { get; set; } = string.Empty;
    public byte[] PackageData { get; set; } = Array.Empty<byte>();
    public string Hash { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public int TenantId { get; set; }
}

public class EvidenceItem
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class SignatureEvidence
{
    public string SignerName { get; set; } = string.Empty;
    public string SignerEmail { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public string SignatureMethod { get; set; } = string.Empty;
    public int SignerId { get; set; }
    public string SignatureHash { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

public class TimestampValidationResult
{
    public bool IsValid { get; set; }
    public string TimestampToken { get; set; } = string.Empty;
    public DateTime TimestampTime { get; set; }
    public string Authority { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
    public Dictionary<string, object> ValidationDetails { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
    public DateTime Timestamp { get; set; }
    public int TenantId { get; set; }
}

public class ComplianceRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public Dictionary<string, object> Conditions { get; set; } = new();
    public Dictionary<string, object> Actions { get; set; } = new();
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public int TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

public class ComplianceStandard
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Requirements { get; set; } = new();
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class AuditTrailReport
{
    public string Id { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public List<AuditTrailEntry> Entries { get; set; } = new();
    public List<AuditTrailEntry> AuditEntries { get; set; } = new();
    public Dictionary<string, object> Summary { get; set; } = new();
    public int TenantId { get; set; }
}

public class AuditTrailEntry
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

public class RetentionPolicy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public int RetentionPeriodDays { get; set; }
    public string RetentionAction { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

public class ComplianceHealthCheck
{
    public int TenantId { get; set; }
    public DateTime CheckedAt { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public Dictionary<string, object> Details { get; set; } = new();
}

public class ComplianceViolation
{
    public string Id { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public int TenantId { get; set; }
}

public class ComplianceMetrics
{
    public int TenantId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalDocuments { get; set; }
    public int CompletedDocuments { get; set; }
    public int ExpiredDocuments { get; set; }
    public int ArchivedDocuments { get; set; }
    public int CompliantDocuments { get; set; }
    public int ViolationCount { get; set; }
    public double ComplianceRate { get; set; }
    public double ExpirationRate { get; set; }
    public double ArchivalRate { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class IdentityVerificationResult
{
    public int SignerId { get; set; }
    public bool IsVerified { get; set; }
    public string VerificationType { get; set; } = string.Empty;
    public Dictionary<string, object> VerificationDetails { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
    public int TenantId { get; set; }
}

public class RegulatoryRequirement
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public List<string> DocumentTypes { get; set; } = new();
    public List<string> Requirements { get; set; } = new();
    public string DocumentType { get; set; } = string.Empty;
    public string Requirement { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime EffectiveDate { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ComplianceEvent
{
    public string Id { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public int UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class CreateComplianceRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public Dictionary<string, object> Conditions { get; set; } = new();
    public Dictionary<string, object> Actions { get; set; } = new();
    public int Priority { get; set; }
}

public class UpdateComplianceRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public Dictionary<string, object> Conditions { get; set; } = new();
    public Dictionary<string, object> Actions { get; set; } = new();
    public bool IsActive { get; set; }
    public int Priority { get; set; }
}

public class CreateRetentionPolicyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public int RetentionPeriodDays { get; set; }
    public string RetentionAction { get; set; } = string.Empty;
}

public class UpdateRetentionPolicyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public int RetentionPeriodDays { get; set; }
    public string RetentionAction { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
