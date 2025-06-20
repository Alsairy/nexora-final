using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;

namespace Nexora.Core.Interfaces
{
    public interface IESignatureComplianceService
    {
        Task<ComplianceValidationResult> ValidateDocumentComplianceAsync(int documentId, int tenantId);
        Task<ComplianceValidationResult> ValidateSignatureComplianceAsync(int signatureId, int tenantId);
        Task<ComplianceValidationResult> ValidateWorkflowComplianceAsync(int workflowId, int tenantId);
        
        Task<LegalEvidencePackage> GenerateLegalEvidencePackageAsync(int documentId, int tenantId);
        Task<byte[]> GenerateComplianceReportAsync(int documentId, int tenantId, string reportFormat = "PDF");
        Task<ComplianceValidationResult> GenerateComplianceCertificateAsync(int documentId, int tenantId);
        
        Task<ComplianceValidationResult> ValidateDigitalSignatureIntegrityAsync(int signatureId, int tenantId);
        Task<ComplianceValidationResult> ValidateDocumentIntegrityAsync(int documentId, int tenantId);
        Task<TimestampValidationResult> ValidateTimestampAsync(string timestampData, int tenantId);
        
        Task<List<ComplianceRule>> GetApplicableComplianceRulesAsync(int documentId, int tenantId);
        Task<List<ComplianceRule>> GetTenantComplianceRulesAsync(int tenantId);
        Task<ComplianceRule> CreateComplianceRuleAsync(CreateComplianceRuleRequest request, int tenantId, int userId);
        Task<ComplianceRule> UpdateComplianceRuleAsync(int ruleId, UpdateComplianceRuleRequest request, int tenantId, int userId);
        Task<ComplianceValidationResult> DeleteComplianceRuleAsync(int ruleId, int tenantId, int userId);
        
        Task<List<ComplianceStandard>> GetSupportedComplianceStandardsAsync();
        Task<ComplianceStandard> GetComplianceStandardAsync(string standardCode);
        Task<ComplianceValidationResult> IsStandardApplicableAsync(string standardCode, int tenantId);
        
        Task<AuditTrailReport> GenerateAuditTrailReportAsync(int documentId, int tenantId);
        Task<List<ESignatureAuditLog>> GetComplianceAuditLogsAsync(int documentId, int tenantId);
        Task LogComplianceEventAsync(ComplianceEvent complianceEvent, int tenantId);
        
        Task<RetentionPolicy> GetDocumentRetentionPolicyAsync(int documentId, int tenantId);
        Task<List<RetentionPolicy>> GetTenantRetentionPoliciesAsync(int tenantId);
        Task<RetentionPolicy> CreateRetentionPolicyAsync(CreateRetentionPolicyRequest request, int tenantId, int userId);
        Task<RetentionPolicy> UpdateRetentionPolicyAsync(int policyId, UpdateRetentionPolicyRequest request, int tenantId, int userId);
        
        Task<ComplianceValidationResult> ArchiveExpiredDocumentsAsync(int tenantId);
        Task<ComplianceValidationResult> AnonymizePersonalDataAsync(int documentId, int tenantId);
        Task<ComplianceValidationResult> PurgeDocumentDataAsync(int documentId, int tenantId, bool force = false);
        
        Task<ComplianceHealthCheck> PerformComplianceHealthCheckAsync(int tenantId);
        Task<List<ComplianceViolation>> GetComplianceViolationsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<ComplianceMetrics> GetComplianceMetricsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
        
        Task<ComplianceValidationResult> ValidateSignerIdentityAsync(int signerId, int tenantId);
        Task<IdentityVerificationResult> PerformIdentityVerificationAsync(int signerId, string verificationType, int tenantId);
        
        Task<List<RegulatoryRequirement>> GetRegulatoryRequirementsAsync(string jurisdiction, string documentType);
        Task<ComplianceValidationResult> ValidateRegulatoryComplianceAsync(int documentId, string jurisdiction, int tenantId);
        
        Task ProcessComplianceScheduledTasksAsync(int tenantId);
        Task SendComplianceNotificationsAsync(int tenantId);
        Task<bool> ValidateSignatureIntegrityAsync(int signatureId, CancellationToken cancellationToken = default);
        Task<byte[]> GenerateSignedDocumentPdfAsync(int documentId, CancellationToken cancellationToken = default);
    }
}
