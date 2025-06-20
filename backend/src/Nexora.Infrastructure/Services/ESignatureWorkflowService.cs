using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Nexora.Core.DTOs;
using Nexora.Core.Models;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Core.Data;
using AuditEntry = Nexora.Core.Models.AuditEntry;

namespace Nexora.Infrastructure.Services
{
    public class ESignatureWorkflowService : IESignatureWorkflowService
    {
        private readonly NexoraDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ESignatureWorkflowService> _logger;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public ESignatureWorkflowService(
            NexoraDbContext context,
            IMapper mapper,
            ILogger<ESignatureWorkflowService> logger,
            IAuditService auditService,
            INotificationService notificationService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        public async Task<ESignatureWorkflowResponse> CreateWorkflowAsync(CreateESignatureWorkflowRequest request, int tenantId, int userId)
        {
            try
            {
                _logger.LogInformation("Creating e-signature workflow for tenant {TenantId} by user {UserId}", tenantId, userId);

                var validationErrors = await GetWorkflowValidationErrorsAsync(request, tenantId);
                if (validationErrors.Any())
                    throw new ArgumentException($"Workflow validation failed: {string.Join(", ", validationErrors)}");

                var workflow = new ESignatureWorkflow
                {
                    TenantId = tenantId,
                    Name = request.Name,
                    Description = request.Description,
                    WorkflowType = request.WorkflowType,
                    IsActive = request.IsActive,
                    IsDefault = request.IsDefault,
                    StepCount = request.Steps.Count,
                    WorkflowDefinition = System.Text.Json.JsonSerializer.Serialize(request),
                    AuthenticationMethod = request.AuthenticationMethod,
                    RequireAllSteps = request.RequireAllSteps,
                    AllowSkipSteps = request.AllowSkipSteps,
                    ExpiryDays = request.ExpiryDays,
                    SendReminders = request.SendReminders,
                    ReminderIntervalHours = request.ReminderIntervalHours,
                    Timestamp = DateTime.UtcNow
                };

                if (request.IsDefault)
                {
                    var existingDefaults = await _context.ESignatureWorkflows
                        .Where(w => w.TenantId == tenantId && w.IsDefault && w.IsActive && !w.IsDeleted)
                        .ToListAsync();

                    foreach (var existing in existingDefaults)
                    {
                        existing.IsDefault = false;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }

                _context.ESignatureWorkflows.Add(workflow);
                await _context.SaveChangesAsync();

                foreach (var stepRequest in request.Steps)
                {
                    var step = new ESignatureWorkflowStep
                    {
                        TenantId = tenantId,
                        WorkflowId = workflow.Id,
                        Name = stepRequest.Name,
                        Description = stepRequest.Description,
                        StepOrder = stepRequest.StepOrder,
                        StepType = stepRequest.StepType,
                        AssigneeType = stepRequest.AssigneeType,
                        AssigneeValue = stepRequest.AssigneeValue,
                        IsRequired = stepRequest.IsRequired,
                        AllowDelegation = stepRequest.AllowDelegation,
                        AuthenticationMethod = stepRequest.AuthenticationMethod,
                        TimeoutHours = stepRequest.TimeoutHours,
                        StepConfiguration = stepRequest.StepConfiguration,
                        Timestamp = DateTime.UtcNow
                    };

                    _context.ESignatureWorkflowSteps.Add(step);
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureWorkflow),
                    EntityId = workflow.Id.ToString(),
                    Action = "Create",
                    Changes = new Dictionary<string, object?> { ["workflow"] = workflow },
                    Timestamp = DateTime.UtcNow
                });

                return await GetWorkflowAsync(workflow.Id, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating e-signature workflow for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<ESignatureWorkflowResponse> GetWorkflowAsync(int workflowId, int tenantId)
        {
            try
            {
                var workflow = await _context.ESignatureWorkflows
                    .Include(w => w.Steps.OrderBy(s => s.StepOrder))
                    .FirstOrDefaultAsync(w => w.Id == workflowId && w.TenantId == tenantId && !w.IsDeleted);

                if (workflow == null)
                    return null;

                var response = _mapper.Map<ESignatureWorkflowResponse>(workflow);
                response.UsageStats = await GetWorkflowUsageStatsInternalAsync(workflowId, tenantId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting e-signature workflow {WorkflowId} for tenant {TenantId}", workflowId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureWorkflowListResponse> GetWorkflowsAsync(ESignatureWorkflowListRequest request, int tenantId)
        {
            try
            {
                var query = _context.ESignatureWorkflows
                    .Include(w => w.Steps)
                    .Where(w => w.TenantId == tenantId && !w.IsDeleted);

                if (request.IsActive.HasValue)
                    query = query.Where(w => w.IsActive == request.IsActive.Value);

                if (request.IsDefault.HasValue)
                    query = query.Where(w => w.IsDefault == request.IsDefault.Value);

                if (!string.IsNullOrEmpty(request.WorkflowType))
                    query = query.Where(w => w.WorkflowType == request.WorkflowType);

                if (!string.IsNullOrEmpty(request.SearchTerm))
                    query = query.Where(w => w.Name.Contains(request.SearchTerm) || w.Description.Contains(request.SearchTerm));

                switch (request.SortBy?.ToLower())
                {
                    case "name":
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(w => w.Name)
                            : query.OrderBy(w => w.Name);
                        break;
                    case "workflowtype":
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(w => w.WorkflowType)
                            : query.OrderBy(w => w.WorkflowType);
                        break;
                    case "createdat":
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(w => w.CreatedAt)
                            : query.OrderBy(w => w.CreatedAt);
                        break;
                    default:
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(w => w.Name)
                            : query.OrderBy(w => w.Name);
                        break;
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var workflows = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var workflowResponses = new List<ESignatureWorkflowResponse>();
                foreach (var workflow in workflows)
                {
                    var response = _mapper.Map<ESignatureWorkflowResponse>(workflow);
                    response.UsageStats = await GetWorkflowUsageStatsInternalAsync(workflow.Id, tenantId);
                    workflowResponses.Add(response);
                }

                return new ESignatureWorkflowListResponse
                {
                    Workflows = workflowResponses,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting e-signature workflows for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<WorkflowExecutionResponse> ExecuteWorkflowStepAsync(ExecuteWorkflowStepRequest request, int tenantId, int userId)
        {
            try
            {
                _logger.LogInformation("Executing workflow step {StepId} for document {DocumentId} by user {UserId}", 
                    request.StepId, request.DocumentId, userId);

                var document = await _context.ESignatureDocuments
                    .Include(d => d.Workflow)
                    .ThenInclude(w => w.Steps.OrderBy(s => s.StepOrder))
                    .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    throw new ArgumentException("Document not found");

                var step = document.Workflow.Steps.FirstOrDefault(s => s.Id == request.StepId);
                if (step == null)
                    throw new ArgumentException("Workflow step not found");

                var canExecute = await CanUserExecuteStepAsync(request.StepId, userId, tenantId);
                if (!canExecute)
                    throw new UnauthorizedAccessException("User is not authorized to execute this step");

                var executedAt = DateTime.UtcNow;
                var nextStep = GetNextWorkflowStep(document.Workflow.Steps.ToList(), step);

                switch (request.Action.ToLower())
                {
                    case "complete":
                        await ProcessStepCompletion(document, step, request, userId, executedAt);
                        break;
                    case "skip":
                        if (!document.Workflow.AllowSkipSteps)
                            throw new InvalidOperationException("Step skipping is not allowed in this workflow");
                        await ProcessStepSkip(document, step, request, userId, executedAt);
                        break;
                    case "delegate":
                        if (!step.AllowDelegation)
                            throw new InvalidOperationException("Delegation is not allowed for this step");
                        await ProcessStepDelegation(document, step, request, userId, executedAt);
                        break;
                    case "reject":
                        await ProcessStepRejection(document, step, request, userId, executedAt);
                        break;
                    default:
                        throw new ArgumentException($"Invalid action: {request.Action}");
                }

                var isWorkflowCompleted = await IsWorkflowCompletedAsync(document.Id, tenantId);
                if (isWorkflowCompleted)
                {
                    document.Status = "Completed";
                    document.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    await SendWorkflowCompletionNotificationsAsync(document, tenantId);
                }

                return new WorkflowExecutionResponse
                {
                    DocumentId = document.Id,
                    StepId = step.Id,
                    StepName = step.Name,
                    Action = request.Action,
                    Status = "Completed",
                    ExecutedAt = executedAt,
                    ExecutedByUserName = await GetUserNameAsync(userId, tenantId),
                    Comments = request.Comments,
                    NextStepName = nextStep?.Name,
                    NextStepId = nextStep?.Id,
                    IsWorkflowCompleted = isWorkflowCompleted
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow step {StepId} for document {DocumentId}", request.StepId, request.DocumentId);
                throw;
            }
        }

        private async Task<ESignatureWorkflowUsageStats> GetWorkflowUsageStatsInternalAsync(int workflowId, int tenantId)
        {
            var totalDocuments = await _context.ESignatureDocuments
                .CountAsync(d => d.WorkflowId == workflowId && d.TenantId == tenantId && !d.IsDeleted);

            var completedDocuments = await _context.ESignatureDocuments
                .CountAsync(d => d.WorkflowId == workflowId && d.TenantId == tenantId && d.Status == "Completed" && !d.IsDeleted);

            var activeDocuments = await _context.ESignatureDocuments
                .CountAsync(d => d.WorkflowId == workflowId && d.TenantId == tenantId && 
                           (d.Status == "Pending" || d.Status == "InProgress") && !d.IsDeleted);

            var completionRate = totalDocuments > 0 ? (decimal)completedDocuments / totalDocuments * 100 : 0;

            var lastUsed = await _context.ESignatureDocuments
                .Where(d => d.WorkflowId == workflowId && d.TenantId == tenantId && !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => d.CreatedAt)
                .FirstOrDefaultAsync();

            return new ESignatureWorkflowUsageStats
            {
                TotalDocuments = totalDocuments,
                CompletedDocuments = completedDocuments,
                ActiveDocuments = activeDocuments,
                CompletionRate = completionRate,
                AverageCompletionTime = 0, // Would calculate from actual completion times
                LastUsed = lastUsed == default ? null : lastUsed
            };
        }

        private ESignatureWorkflowStep GetNextWorkflowStep(List<ESignatureWorkflowStep> steps, ESignatureWorkflowStep currentStep)
        {
            return steps
                .Where(s => s.StepOrder > currentStep.StepOrder)
                .OrderBy(s => s.StepOrder)
                .FirstOrDefault();
        }

        private async Task ProcessStepCompletion(ESignatureDocument document, ESignatureWorkflowStep step, 
            ExecuteWorkflowStepRequest request, int userId, DateTime executedAt)
        {
            await Task.CompletedTask;
        }

        private async Task ProcessStepSkip(ESignatureDocument document, ESignatureWorkflowStep step, 
            ExecuteWorkflowStepRequest request, int userId, DateTime executedAt)
        {
            await Task.CompletedTask;
        }

        private async Task ProcessStepDelegation(ESignatureDocument document, ESignatureWorkflowStep step, 
            ExecuteWorkflowStepRequest request, int userId, DateTime executedAt)
        {
            await Task.CompletedTask;
        }

        private async Task ProcessStepRejection(ESignatureDocument document, ESignatureWorkflowStep step, 
            ExecuteWorkflowStepRequest request, int userId, DateTime executedAt)
        {
            await Task.CompletedTask;
        }

        private async Task<bool> IsWorkflowCompletedAsync(int documentId, int tenantId)
        {
            return await Task.FromResult(false); // Placeholder implementation
        }

        private async Task SendWorkflowCompletionNotificationsAsync(ESignatureDocument document, int tenantId)
        {
            await Task.CompletedTask;
        }

        private async Task<string> GetUserNameAsync(int userId, int tenantId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId && !u.IsDeleted);
            
            return user?.FullName ?? "Unknown User";
        }

        public async Task<ESignatureWorkflowResponse> UpdateWorkflowAsync(int workflowId, UpdateESignatureWorkflowRequest request, int tenantId, int userId)
        {
            try
            {
                var workflow = await _context.ESignatureWorkflows
                    .FirstOrDefaultAsync(w => w.Id == workflowId && w.TenantId == tenantId && !w.IsDeleted);

                if (workflow == null)
                    throw new ArgumentException("Workflow not found");

                var oldValues = System.Text.Json.JsonSerializer.Serialize(workflow);

                if (!string.IsNullOrEmpty(request.Name))
                    workflow.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Description))
                    workflow.Description = request.Description;
                if (request.IsActive.HasValue)
                    workflow.IsActive = request.IsActive.Value;
                if (!string.IsNullOrEmpty(request.AuthenticationMethod))
                    workflow.AuthenticationMethod = request.AuthenticationMethod;
                if (request.RequireAllSteps.HasValue)
                    workflow.RequireAllSteps = request.RequireAllSteps.Value;
                if (request.AllowSkipSteps.HasValue)
                    workflow.AllowSkipSteps = request.AllowSkipSteps.Value;
                if (request.ExpiryDays.HasValue)
                    workflow.ExpiryDays = request.ExpiryDays.Value;
                if (request.SendReminders.HasValue)
                    workflow.SendReminders = request.SendReminders.Value;
                if (request.ReminderIntervalHours.HasValue)
                    workflow.ReminderIntervalHours = request.ReminderIntervalHours.Value;

                workflow.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureWorkflow),
                    EntityId = workflow.Id.ToString(),
                    Action = "Update",

                    Changes = new Dictionary<string, object?> { ["Workflow"] = workflow },
                    Timestamp = DateTime.UtcNow
                });

                return await GetWorkflowAsync(workflowId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow {WorkflowId} for tenant {TenantId}", workflowId, tenantId);
                throw;
            }
        }

        public async Task<bool> DeleteWorkflowAsync(int workflowId, int tenantId, int userId)
        {
            try
            {
                var workflow = await _context.ESignatureWorkflows
                    .FirstOrDefaultAsync(w => w.Id == workflowId && w.TenantId == tenantId && !w.IsDeleted);

                if (workflow == null)
                    return false;

                var documentsUsingWorkflow = await _context.ESignatureDocuments
                    .AnyAsync(d => d.WorkflowId == workflowId && d.TenantId == tenantId && !d.IsDeleted);

                if (documentsUsingWorkflow)
                    throw new InvalidOperationException("Cannot delete workflow that is being used by documents");

                workflow.IsDeleted = true;
                workflow.DeletedAt = DateTime.UtcNow;
                workflow.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureWorkflow),
                    EntityId = workflow.Id.ToString(),
                    Action = "Delete",
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workflow {WorkflowId} for tenant {TenantId}", workflowId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureWorkflowStepResponse> AddWorkflowStepAsync(int workflowId, CreateESignatureWorkflowStepRequest request, int tenantId, int userId)
        {
            try
            {
                var workflow = await _context.ESignatureWorkflows
                    .FirstOrDefaultAsync(w => w.Id == workflowId && w.TenantId == tenantId && !w.IsDeleted);

                if (workflow == null)
                    throw new ArgumentException("Workflow not found");

                var step = new ESignatureWorkflowStep
                {
                    TenantId = tenantId,
                    WorkflowId = workflowId,
                    Name = request.Name,
                    Description = request.Description,
                    StepOrder = request.StepOrder,
                    StepType = request.StepType,
                    AssigneeType = request.AssigneeType,
                    AssigneeValue = request.AssigneeValue,
                    IsRequired = request.IsRequired,
                    AllowDelegation = request.AllowDelegation,
                    AuthenticationMethod = request.AuthenticationMethod,
                    TimeoutHours = request.TimeoutHours,
                    StepConfiguration = request.StepConfiguration,
                    Timestamp = DateTime.UtcNow
                };

                _context.ESignatureWorkflowSteps.Add(step);

                workflow.StepCount = await _context.ESignatureWorkflowSteps
                    .CountAsync(s => s.WorkflowId == workflowId && s.TenantId == tenantId && !s.IsDeleted) + 1;
                workflow.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureWorkflowStep),
                    EntityId = step.Id.ToString(),
                    Action = "Create",
                    Changes = new Dictionary<string, object?> { ["Step"] = step },
                    Timestamp = DateTime.UtcNow
                });

                return _mapper.Map<ESignatureWorkflowStepResponse>(step);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding step to workflow {WorkflowId} for tenant {TenantId}", workflowId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureWorkflowStepResponse> UpdateWorkflowStepAsync(int stepId, UpdateESignatureWorkflowStepRequest request, int tenantId, int userId)
        {
            try
            {
                var step = await _context.ESignatureWorkflowSteps
                    .FirstOrDefaultAsync(s => s.Id == stepId && s.TenantId == tenantId && !s.IsDeleted);

                if (step == null)
                    throw new ArgumentException("Workflow step not found");

                var oldValues = System.Text.Json.JsonSerializer.Serialize(step);

                if (!string.IsNullOrEmpty(request.Name))
                    step.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Description))
                    step.Description = request.Description;
                if (request.StepOrder.HasValue)
                    step.StepOrder = request.StepOrder.Value;
                if (!string.IsNullOrEmpty(request.StepType))
                    step.StepType = request.StepType;
                if (!string.IsNullOrEmpty(request.AssigneeType))
                    step.AssigneeType = request.AssigneeType;
                if (!string.IsNullOrEmpty(request.AssigneeValue))
                    step.AssigneeValue = request.AssigneeValue;
                if (request.IsRequired.HasValue)
                    step.IsRequired = request.IsRequired.Value;
                if (request.AllowDelegation.HasValue)
                    step.AllowDelegation = request.AllowDelegation.Value;
                if (!string.IsNullOrEmpty(request.AuthenticationMethod))
                    step.AuthenticationMethod = request.AuthenticationMethod;
                if (request.TimeoutHours.HasValue)
                    step.TimeoutHours = request.TimeoutHours.Value;
                if (!string.IsNullOrEmpty(request.StepConfiguration))
                    step.StepConfiguration = request.StepConfiguration;

                step.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureWorkflowStep),
                    EntityId = step.Id.ToString(),
                    Action = "Update",

                    Changes = new Dictionary<string, object?> { ["Step"] = step },
                    Timestamp = DateTime.UtcNow
                });

                return _mapper.Map<ESignatureWorkflowStepResponse>(step);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow step {StepId} for tenant {TenantId}", stepId, tenantId);
                throw;
            }
        }

        public async Task<bool> RemoveWorkflowStepAsync(int stepId, int tenantId, int userId)
        {
            try
            {
                var step = await _context.ESignatureWorkflowSteps
                    .FirstOrDefaultAsync(s => s.Id == stepId && s.TenantId == tenantId && !s.IsDeleted);

                if (step == null)
                    return false;

                step.IsDeleted = true;
                step.DeletedAt = DateTime.UtcNow;
                step.UpdatedAt = DateTime.UtcNow;

                var workflow = await _context.ESignatureWorkflows
                    .FirstOrDefaultAsync(w => w.Id == step.WorkflowId && w.TenantId == tenantId && !w.IsDeleted);

                if (workflow != null)
                {
                    workflow.StepCount = await _context.ESignatureWorkflowSteps
                        .CountAsync(s => s.WorkflowId == workflow.Id && s.TenantId == tenantId && !s.IsDeleted) - 1;
                    workflow.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureWorkflowStep),
                    EntityId = step.Id.ToString(),
                    Action = "Delete",
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing workflow step {StepId} for tenant {TenantId}", stepId, tenantId);
                throw;
            }
        }

        public async Task<List<ESignatureWorkflowStepResponse>> GetWorkflowStepsAsync(int workflowId, int tenantId)
        {
            try
            {
                var steps = await _context.ESignatureWorkflowSteps
                    .Where(s => s.WorkflowId == workflowId && s.TenantId == tenantId && !s.IsDeleted)
                    .OrderBy(s => s.StepOrder)
                    .ToListAsync();

                return _mapper.Map<List<ESignatureWorkflowStepResponse>>(steps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow steps for workflow {WorkflowId} for tenant {TenantId}", workflowId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureWorkflowStepResponse> GetWorkflowStepAsync(int stepId, int tenantId)
        {
            try
            {
                var step = await _context.ESignatureWorkflowSteps
                    .FirstOrDefaultAsync(s => s.Id == stepId && s.TenantId == tenantId && !s.IsDeleted);

                if (step == null)
                    return null;

                return _mapper.Map<ESignatureWorkflowStepResponse>(step);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow step {StepId} for tenant {TenantId}", stepId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureWorkflowResponse> ActivateWorkflowAsync(int workflowId, int tenantId, int userId)
        {
            try
            {
                var workflow = await _context.ESignatureWorkflows
                    .FirstOrDefaultAsync(w => w.Id == workflowId && w.TenantId == tenantId && !w.IsDeleted);

                if (workflow == null)
                    throw new ArgumentException("Workflow not found");

                workflow.IsActive = true;
                workflow.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureWorkflow),
                    EntityId = workflow.Id.ToString(),
                    Action = "Activate",
                    Timestamp = DateTime.UtcNow
                });

                return await GetWorkflowAsync(workflowId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating workflow {WorkflowId} for tenant {TenantId}", workflowId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureWorkflowResponse> DeactivateWorkflowAsync(int workflowId, int tenantId, int userId)
        {
            try
            {
                var workflow = await _context.ESignatureWorkflows
                    .FirstOrDefaultAsync(w => w.Id == workflowId && w.TenantId == tenantId && !w.IsDeleted);

                if (workflow == null)
                    throw new ArgumentException("Workflow not found");

                workflow.IsActive = false;
                workflow.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureWorkflow),
                    EntityId = workflow.Id.ToString(),
                    Action = "Deactivate",
                    Timestamp = DateTime.UtcNow
                });

                return await GetWorkflowAsync(workflowId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating workflow {WorkflowId} for tenant {TenantId}", workflowId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureWorkflowResponse> SetDefaultWorkflowAsync(int workflowId, int tenantId, int userId)
        {
            try
            {
                var workflow = await _context.ESignatureWorkflows
                    .FirstOrDefaultAsync(w => w.Id == workflowId && w.TenantId == tenantId && !w.IsDeleted);

                if (workflow == null)
                    throw new ArgumentException("Workflow not found");

                var existingDefaults = await _context.ESignatureWorkflows
                    .Where(w => w.TenantId == tenantId && w.IsDefault && w.IsActive && !w.IsDeleted)
                    .ToListAsync();

                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                    existing.UpdatedAt = DateTime.UtcNow;
                }

                workflow.IsDefault = true;
                workflow.IsActive = true;
                workflow.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureWorkflow),
                    EntityId = workflow.Id.ToString(),
                    Action = "SetDefault",
                    Timestamp = DateTime.UtcNow
                });

                return await GetWorkflowAsync(workflowId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default workflow {WorkflowId} for tenant {TenantId}", workflowId, tenantId);
                throw;
            }
        }

        public async Task<WorkflowInstanceResponse> GetWorkflowInstanceAsync(int documentId, int tenantId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .Include(d => d.Workflow)
                    .ThenInclude(w => w.Steps.OrderBy(s => s.StepOrder))
                    .Include(d => d.Signers)
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    return null;

                var currentStep = GetCurrentWorkflowStep(document);
                var nextStep = GetNextWorkflowStep(document.Workflow.Steps.ToList(), currentStep);

                return new WorkflowInstanceResponse
                {
                    DocumentId = documentId,
                    WorkflowId = document.WorkflowId,
                    WorkflowName = document.Workflow.Name,
                    Status = document.Status,
                    CurrentStepId = currentStep?.Id,
                    CurrentStepName = currentStep?.Name,
                    NextStepId = nextStep?.Id,
                    NextStepName = nextStep?.Name,
                    CompletedSteps = GetCompletedStepsCount(document),
                    TotalSteps = document.Workflow.Steps.Count,
                    StartedAt = document.CreatedAt,
                    LastActivityAt = document.UpdatedAt ?? document.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow instance for document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<List<WorkflowInstanceResponse>> GetActiveWorkflowInstancesAsync(int tenantId)
        {
            try
            {
                var activeDocuments = await _context.ESignatureDocuments
                    .Include(d => d.Workflow)
                    .ThenInclude(w => w.Steps)
                    .Include(d => d.Signers)
                    .Where(d => d.TenantId == tenantId && 
                               (d.Status == "Pending" || d.Status == "InProgress") && 
                               !d.IsDeleted)
                    .ToListAsync();

                var instances = new List<WorkflowInstanceResponse>();
                foreach (var document in activeDocuments)
                {
                    var instance = await GetWorkflowInstanceAsync(document.Id, tenantId);
                    if (instance != null)
                        instances.Add(instance);
                }

                return instances;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active workflow instances for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> ValidateWorkflowDefinitionAsync(CreateESignatureWorkflowRequest request, int tenantId)
        {
            try
            {
                var errors = await GetWorkflowValidationErrorsAsync(request, tenantId);
                return !errors.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating workflow definition for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<List<string>> GetWorkflowValidationErrorsAsync(CreateESignatureWorkflowRequest request, int tenantId)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(request.Name))
                errors.Add("Workflow name is required");

            if (request.Steps == null || !request.Steps.Any())
                errors.Add("At least one workflow step is required");

            if (request.Steps != null)
            {
                var stepOrders = request.Steps.Select(s => s.StepOrder).ToList();
                if (stepOrders.Count != stepOrders.Distinct().Count())
                    errors.Add("Step orders must be unique");

                if (stepOrders.Any(o => o <= 0))
                    errors.Add("Step orders must be greater than 0");
            }

            return errors;
        }

        public async Task<ESignatureWorkflowResponse> CloneWorkflowAsync(int workflowId, string newName, int tenantId, int userId)
        {
            try
            {
                var originalWorkflow = await _context.ESignatureWorkflows
                    .Include(w => w.Steps)
                    .FirstOrDefaultAsync(w => w.Id == workflowId && w.TenantId == tenantId && !w.IsDeleted);

                if (originalWorkflow == null)
                    throw new ArgumentException("Workflow not found");

                var clonedWorkflow = new ESignatureWorkflow
                {
                    TenantId = tenantId,
                    Name = newName,
                    Description = $"Cloned from {originalWorkflow.Name}",
                    WorkflowType = originalWorkflow.WorkflowType,
                    IsActive = false,
                    IsDefault = false,
                    StepCount = originalWorkflow.StepCount,
                    WorkflowDefinition = originalWorkflow.WorkflowDefinition,
                    AuthenticationMethod = originalWorkflow.AuthenticationMethod,
                    RequireAllSteps = originalWorkflow.RequireAllSteps,
                    AllowSkipSteps = originalWorkflow.AllowSkipSteps,
                    ExpiryDays = originalWorkflow.ExpiryDays,
                    SendReminders = originalWorkflow.SendReminders,
                    ReminderIntervalHours = originalWorkflow.ReminderIntervalHours,
                    Timestamp = DateTime.UtcNow
                };

                _context.ESignatureWorkflows.Add(clonedWorkflow);
                await _context.SaveChangesAsync();

                foreach (var originalStep in originalWorkflow.Steps)
                {
                    var clonedStep = new ESignatureWorkflowStep
                    {
                        TenantId = tenantId,
                        WorkflowId = clonedWorkflow.Id,
                        Name = originalStep.Name,
                        Description = originalStep.Description,
                        StepOrder = originalStep.StepOrder,
                        StepType = originalStep.StepType,
                        AssigneeType = originalStep.AssigneeType,
                        AssigneeValue = originalStep.AssigneeValue,
                        IsRequired = originalStep.IsRequired,
                        AllowDelegation = originalStep.AllowDelegation,
                        AuthenticationMethod = originalStep.AuthenticationMethod,
                        TimeoutHours = originalStep.TimeoutHours,
                        StepConfiguration = originalStep.StepConfiguration,
                        Timestamp = DateTime.UtcNow
                    };

                    _context.ESignatureWorkflowSteps.Add(clonedStep);
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureWorkflow),
                    EntityId = clonedWorkflow.Id.ToString(),
                    Action = "Clone",
                    Changes = new Dictionary<string, object?> { ["OriginalWorkflowId"] = workflowId, ["NewName"] = newName },
                    Timestamp = DateTime.UtcNow
                });

                return await GetWorkflowAsync(clonedWorkflow.Id, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning workflow {WorkflowId} for tenant {TenantId}", workflowId, tenantId);
                throw;
            }
        }

        public async Task<List<ESignatureWorkflowResponse>> GetWorkflowTemplatesAsync(int tenantId)
        {
            try
            {
                var templates = await _context.ESignatureWorkflows
                    .Include(w => w.Steps)
                    .Where(w => w.TenantId == tenantId && w.IsActive && !w.IsDeleted)
                    .OrderBy(w => w.Name)
                    .ToListAsync();

                var responses = new List<ESignatureWorkflowResponse>();
                foreach (var template in templates)
                {
                    var response = _mapper.Map<ESignatureWorkflowResponse>(template);
                    response.UsageStats = await GetWorkflowUsageStatsInternalAsync(template.Id, tenantId);
                    responses.Add(response);
                }

                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow templates for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task ProcessWorkflowTimeoutsAsync(int tenantId)
        {
            try
            {
                var timedOutSteps = await _context.ESignatureWorkflowSteps
                    .Include(s => s.Workflow)
                    .Where(s => s.TenantId == tenantId && 
                               s.TimeoutHours.HasValue && 
                               s.CreatedAt.AddHours(s.TimeoutHours.Value) < DateTime.UtcNow &&
                               !s.IsDeleted)
                    .ToListAsync();

                foreach (var step in timedOutSteps)
                {
                    await ProcessStepTimeout(step, tenantId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing workflow timeouts for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task SendWorkflowRemindersAsync(int tenantId)
        {
            try
            {
                var pendingDocuments = await _context.ESignatureDocuments
                    .Include(d => d.Workflow)
                    .Include(d => d.Signers)
                    .Where(d => d.TenantId == tenantId && 
                               d.Status == "Pending" && 
                               d.Workflow.SendReminders &&
                               !d.IsDeleted)
                    .ToListAsync();

                foreach (var document in pendingDocuments)
                {
                    var hoursSinceLastActivity = (DateTime.UtcNow - (document.UpdatedAt ?? document.CreatedAt)).TotalHours;
                    
                    if (hoursSinceLastActivity >= document.Workflow.ReminderIntervalHours)
                    {
                        await SendDocumentReminderAsync(document, tenantId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending workflow reminders for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> CanUserExecuteStepAsync(int stepId, int userId, int tenantId)
        {
            var step = await _context.ESignatureWorkflowSteps
                .FirstOrDefaultAsync(s => s.Id == stepId && s.TenantId == tenantId && !s.IsDeleted);

            if (step == null)
                return false;

            return true;
        }

        public async Task<List<WorkflowStepInstanceResponse>> GetUserPendingStepsAsync(int userId, int tenantId)
        {
            try
            {
                var pendingSteps = await _context.ESignatureSigners
                    .Include(s => s.Document)
                    .ThenInclude(d => d.Workflow)
                    .ThenInclude(w => w.Steps)
                    .Where(s => s.TenantId == tenantId && 
                               (s.Status == "Invited" || s.Status == "Viewed") &&
                               !s.IsDeleted && !s.Document.IsDeleted)
                    .ToListAsync();

                var stepInstances = new List<WorkflowStepInstanceResponse>();
                foreach (var signer in pendingSteps)
                {
                    var currentStep = GetCurrentWorkflowStepForSigner(signer);
                    if (currentStep != null)
                    {
                        stepInstances.Add(new WorkflowStepInstanceResponse
                        {
                            DocumentId = signer.DocumentId,
                            DocumentTitle = signer.Document.Title,
                            StepId = currentStep.Id,
                            StepName = currentStep.Name,
                            StepType = currentStep.StepType,
                            SignerId = signer.Id,
                            SignerName = signer.FullName,
                            AssignedAt = signer.InvitedAt,
                            DueDate = signer.Document.ExpiryDate,
                            Priority = GetStepPriority(signer.Document, currentStep),
                            Status = signer.Status
                        });
                    }
                }

                return stepInstances.OrderBy(s => s.DueDate).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user pending steps for user {UserId} tenant {TenantId}", userId, tenantId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetWorkflowMetricsAsync(int workflowId, int tenantId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.ESignatureDocuments
                    .Where(d => d.WorkflowId == workflowId && d.TenantId == tenantId && !d.IsDeleted);

                if (fromDate.HasValue)
                    query = query.Where(d => d.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(d => d.CreatedAt <= toDate.Value);

                var documents = await query.ToListAsync();

                var metrics = new Dictionary<string, object>
                {
                    ["TotalDocuments"] = documents.Count,
                    ["CompletedDocuments"] = documents.Count(d => d.Status == "Completed"),
                    ["PendingDocuments"] = documents.Count(d => d.Status == "Pending"),
                    ["InProgressDocuments"] = documents.Count(d => d.Status == "InProgress"),
                    ["ExpiredDocuments"] = documents.Count(d => d.Status == "Expired"),
                    ["CancelledDocuments"] = documents.Count(d => d.Status == "Cancelled"),
                    ["AverageCompletionTime"] = CalculateAverageCompletionTime(documents.Where(d => d.Status == "Completed")),
                    ["CompletionRate"] = documents.Count > 0 ? (decimal)documents.Count(d => d.Status == "Completed") / documents.Count * 100 : 0,
                    ["DocumentsByStatus"] = documents.GroupBy(d => d.Status).ToDictionary(g => g.Key, g => g.Count()),
                    ["DocumentsByMonth"] = documents.GroupBy(d => new { d.CreatedAt.Year, d.CreatedAt.Month })
                        .ToDictionary(g => $"{g.Key.Year}-{g.Key.Month:D2}", g => g.Count())
                };

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow metrics for workflow {WorkflowId} tenant {TenantId}", workflowId, tenantId);
                throw;
            }
        }

        public async Task<List<ESignatureWorkflowUsageStats>> GetWorkflowUsageStatsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var workflows = await _context.ESignatureWorkflows
                    .Where(w => w.TenantId == tenantId && !w.IsDeleted)
                    .ToListAsync();

                var usageStats = new List<ESignatureWorkflowUsageStats>();
                foreach (var workflow in workflows)
                {
                    var stats = await GetWorkflowUsageStatsInternalAsync(workflow.Id, tenantId);
                    usageStats.Add(stats);
                }

                return usageStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow usage stats for tenant {TenantId}", tenantId);
                throw;
            }
        }

        private ESignatureWorkflowStep GetCurrentWorkflowStep(ESignatureDocument document)
        {
            var pendingSigners = document.Signers.Where(s => s.Status == "Invited" || s.Status == "Viewed").ToList();
            if (!pendingSigners.Any())
                return null;

            var minOrder = pendingSigners.Min(s => s.SigningOrder);
            var currentSigner = pendingSigners.FirstOrDefault(s => s.SigningOrder == minOrder);
            
            return document.Workflow.Steps.FirstOrDefault(s => s.StepOrder == currentSigner?.SigningOrder);
        }

        private ESignatureWorkflowStep GetCurrentWorkflowStepForSigner(ESignatureSigner signer)
        {
            return signer.Document.Workflow.Steps.FirstOrDefault(s => s.StepOrder == signer.SigningOrder);
        }

        private int GetCompletedStepsCount(ESignatureDocument document)
        {
            return document.Signers.Count(s => s.Status == "Signed");
        }

        private string GetStepPriority(ESignatureDocument document, ESignatureWorkflowStep step)
        {
            if (document.ExpiryDate.HasValue && document.ExpiryDate.Value <= DateTime.UtcNow.AddDays(1))
                return "High";
            
            if (step.TimeoutHours.HasValue && step.CreatedAt.AddHours(step.TimeoutHours.Value) <= DateTime.UtcNow.AddHours(24))
                return "Medium";

            return "Normal";
        }

        private async Task ProcessStepTimeout(ESignatureWorkflowStep step, int tenantId)
        {
            await _notificationService.SendNotificationAsync(new NotificationRequest
            {
                TenantId = tenantId,
                Subject = "Workflow Step Timeout",
                Message = $"Workflow step '{step.Name}' has timed out",
                NotificationType = "System"
            });
        }

        private async Task SendDocumentReminderAsync(ESignatureDocument document, int tenantId)
        {
            var pendingSigners = document.Signers.Where(s => s.Status == "Invited" || s.Status == "Viewed").ToList();
            
            foreach (var signer in pendingSigners)
            {
                await _notificationService.SendNotificationAsync(new NotificationRequest
                {
                    TenantId = tenantId,
                    RecipientEmail = signer.Email,
                    Subject = "Document Signing Reminder",
                    Message = $"This is a reminder to sign the document: {document.Title}",
                    NotificationType = "Email"
                });
            }
        }

        private decimal CalculateAverageCompletionTime(IEnumerable<ESignatureDocument> completedDocuments)
        {
            var completionTimes = completedDocuments
                .Where(d => d.UpdatedAt.HasValue)
                .Select(d => (d.UpdatedAt.Value - d.CreatedAt).TotalHours)
                .ToList();

            return completionTimes.Any() ? (decimal)completionTimes.Average() : 0;
        }
    }
}
