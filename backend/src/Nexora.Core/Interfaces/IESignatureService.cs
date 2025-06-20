using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;

namespace Nexora.Core.Interfaces
{
    public interface IESignatureService
    {
        Task<ESignatureDocumentResponse> CreateDocumentAsync(CreateESignatureDocumentRequest request, int tenantId, int userId);
        Task<ESignatureDocumentResponse> UpdateDocumentAsync(int documentId, UpdateESignatureDocumentRequest request, int tenantId, int userId);
        Task<ESignatureDocumentResponse> GetDocumentAsync(int documentId, int tenantId);
        Task<ESignatureListResponse> GetDocumentsAsync(ESignatureListRequest request, int tenantId);
        Task<bool> DeleteDocumentAsync(int documentId, int tenantId, int userId);
        
        Task<ESignatureDocumentResponse> SendDocumentForSigningAsync(SendDocumentForSigningRequest request, int tenantId, int userId);
        Task<ESignatureDocumentResponse> CancelDocumentAsync(int documentId, string reason, int tenantId, int userId);
        Task<ESignatureDocumentResponse> ExpireDocumentAsync(int documentId, int tenantId, int userId);
        
        Task<ESignatureSignerResponse> AddSignerAsync(int documentId, CreateESignatureSignerRequest request, int tenantId, int userId);
        Task<ESignatureSignerResponse> UpdateSignerAsync(int signerId, UpdateESignatureSignerRequest request, int tenantId, int userId);
        Task<bool> RemoveSignerAsync(int signerId, int tenantId, int userId);
        Task<ESignatureSignerResponse> GetSignerAsync(int signerId, int tenantId);
        Task<List<ESignatureSignerResponse>> GetDocumentSignersAsync(int documentId, int tenantId);
        
        Task<ESignatureSignatureResponse> CreateSignatureAsync(CreateSignatureRequest request, int tenantId);
        Task<ESignatureSignatureResponse> GetSignatureAsync(int signatureId, int tenantId);
        Task<List<ESignatureSignatureResponse>> GetDocumentSignaturesAsync(int documentId, int tenantId);
        Task<bool> ValidateSignatureAsync(int signatureId, int tenantId);
        
        Task<ESignatureDocumentResponse> DeclineSigningAsync(DeclineSigningRequest request, int tenantId);
        Task<ESignatureDocumentResponse> DelegateSigningAsync(DelegateSigningRequest request, int tenantId, int userId);
        
        Task<ESignatureProgressResponse> GetDocumentProgressAsync(int documentId, int tenantId);
        Task<ESignatureStatisticsResponse> GetStatisticsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
        
        Task<byte[]> GenerateDocumentPdfAsync(int documentId, int tenantId);
        Task<byte[]> GenerateLegalEvidencePackageAsync(int documentId, int tenantId);
        Task<string> GenerateSigningLinkAsync(int documentId, int signerId, int tenantId);
        
        Task<bool> ValidateDocumentIntegrityAsync(int documentId, int tenantId);
        Task<List<ESignatureAuditLog>> GetDocumentAuditTrailAsync(int documentId, int tenantId);
        
        Task SendReminderNotificationsAsync(int tenantId);
        Task ProcessExpiredDocumentsAsync(int tenantId);
        Task<bool> VerifySignerAccessAsync(int documentId, int signerId, string token, int tenantId);
    }
}
