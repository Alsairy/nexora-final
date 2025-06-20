using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;

namespace Nexora.Core.Interfaces
{
    public interface IESignatureWorkflowService
    {
        Task<ESignatureWorkflowResponse> CreateWorkflowAsync(CreateESignatureWorkflowRequest request, int tenantId, int userId);
        Task<ESignatureWorkflowResponse> UpdateWorkflowAsync(int workflowId, UpdateESignatureWorkflowRequest request, int tenantId, int userId);
        Task<ESignatureWorkflowResponse> GetWorkflowAsync(int workflowId, int tenantId);
        Task<ESignatureWorkflowListResponse> GetWorkflowsAsync(ESignatureWorkflowListRequest request, int tenantId);
        Task<bool> DeleteWorkflowAsync(int workflowId, int tenantId, int userId);
        
        Task<ESignatureWorkflowStepResponse> AddWorkflowStepAsync(int workflowId, CreateESignatureWorkflowStepRequest request, int tenantId, int userId);
        Task<ESignatureWorkflowStepResponse> UpdateWorkflowStepAsync(int stepId, UpdateESignatureWorkflowStepRequest request, int tenantId, int userId);
        Task<bool> RemoveWorkflowStepAsync(int stepId, int tenantId, int userId);
        Task<List<ESignatureWorkflowStepResponse>> GetWorkflowStepsAsync(int workflowId, int tenantId);
        Task<ESignatureWorkflowStepResponse> GetWorkflowStepAsync(int stepId, int tenantId);
        
        Task<ESignatureWorkflowResponse> ActivateWorkflowAsync(int workflowId, int tenantId, int userId);
        Task<ESignatureWorkflowResponse> DeactivateWorkflowAsync(int workflowId, int tenantId, int userId);
        Task<ESignatureWorkflowResponse> SetDefaultWorkflowAsync(int workflowId, int tenantId, int userId);
        
        Task<WorkflowExecutionResponse> ExecuteWorkflowStepAsync(ExecuteWorkflowStepRequest request, int tenantId, int userId);
        Task<WorkflowInstanceResponse> GetWorkflowInstanceAsync(int documentId, int tenantId);
        Task<List<WorkflowInstanceResponse>> GetActiveWorkflowInstancesAsync(int tenantId);
        
        Task<bool> ValidateWorkflowDefinitionAsync(CreateESignatureWorkflowRequest request, int tenantId);
        Task<List<string>> GetWorkflowValidationErrorsAsync(CreateESignatureWorkflowRequest request, int tenantId);
        
        Task<ESignatureWorkflowResponse> CloneWorkflowAsync(int workflowId, string newName, int tenantId, int userId);
        Task<List<ESignatureWorkflowResponse>> GetWorkflowTemplatesAsync(int tenantId);
        
        Task ProcessWorkflowTimeoutsAsync(int tenantId);
        Task SendWorkflowRemindersAsync(int tenantId);
        
        Task<bool> CanUserExecuteStepAsync(int stepId, int userId, int tenantId);
        Task<List<WorkflowStepInstanceResponse>> GetUserPendingStepsAsync(int userId, int tenantId);
        
        Task<Dictionary<string, object>> GetWorkflowMetricsAsync(int workflowId, int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<ESignatureWorkflowUsageStats>> GetWorkflowUsageStatsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
    }
}
