using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;

namespace Nexora.Core.Interfaces
{
    public interface IESignatureTemplateService
    {
        Task<ESignatureTemplateResponse> CreateTemplateAsync(CreateESignatureTemplateRequest request, int tenantId, int userId);
        Task<ESignatureTemplateResponse> UpdateTemplateAsync(int templateId, UpdateESignatureTemplateRequest request, int tenantId, int userId);
        Task<ESignatureTemplateResponse> GetTemplateAsync(int templateId, int tenantId);
        Task<ESignatureTemplateListResponse> GetTemplatesAsync(ESignatureTemplateListRequest request, int tenantId);
        Task<bool> DeleteTemplateAsync(int templateId, int tenantId, int userId);
        
        Task<ESignatureTemplateFieldResponse> AddTemplateFieldAsync(int templateId, CreateESignatureTemplateFieldRequest request, int tenantId, int userId);
        Task<ESignatureTemplateFieldResponse> UpdateTemplateFieldAsync(int fieldId, UpdateESignatureTemplateFieldRequest request, int tenantId, int userId);
        Task<bool> RemoveTemplateFieldAsync(int fieldId, int tenantId, int userId);
        Task<List<ESignatureTemplateFieldResponse>> GetTemplateFieldsAsync(int templateId, int tenantId);
        Task<ESignatureTemplateFieldResponse> GetTemplateFieldAsync(int fieldId, int tenantId);
        
        Task<ESignatureTemplateResponse> ActivateTemplateAsync(int templateId, int tenantId, int userId);
        Task<ESignatureTemplateResponse> DeactivateTemplateAsync(int templateId, int tenantId, int userId);
        Task<ESignatureTemplateResponse> CloneTemplateAsync(int templateId, string newName, int tenantId, int userId);
        
        Task<ESignatureDocumentResponse> CreateDocumentFromTemplateAsync(CreateDocumentFromTemplateRequest request, int tenantId, int userId);
        Task<ESignatureTemplatePreviewResponse> GenerateTemplatePreviewAsync(ESignatureTemplatePreviewRequest request, int tenantId);
        
        Task<bool> ValidateTemplateDefinitionAsync(CreateESignatureTemplateRequest request, int tenantId);
        Task<List<string>> GetTemplateValidationErrorsAsync(CreateESignatureTemplateRequest request, int tenantId);
        
        Task<List<string>> GetTemplateCategoriesAsync(int tenantId);
        Task<List<string>> GetTemplateTagsAsync(int tenantId);
        Task<ESignatureTemplateUsageStats> GetTemplateUsageStatsAsync(int templateId, int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
        
        Task<byte[]> ExportTemplateAsync(int templateId, string format, int tenantId);
        Task<ESignatureTemplateResponse> ImportTemplateAsync(byte[] templateData, string format, int tenantId, int userId);
        
        Task<bool> ValidateTemplateFieldsAsync(int templateId, Dictionary<string, string> fieldValues, int tenantId);
        Task<Dictionary<string, object>> GetTemplateMetricsAsync(int templateId, int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
        
        Task ProcessTemplateMaintenanceAsync(int tenantId);
        Task UpdateTemplateUsageStatsAsync(int templateId, int tenantId);
    }
}
