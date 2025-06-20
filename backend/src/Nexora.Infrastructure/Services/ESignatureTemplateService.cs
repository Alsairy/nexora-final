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
    public class ESignatureTemplateService : IESignatureTemplateService
    {
        private readonly NexoraDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ESignatureTemplateService> _logger;
        private readonly IAuditService _auditService;
        private readonly IESignatureService _eSignatureService;

        public ESignatureTemplateService(
            NexoraDbContext context,
            IMapper mapper,
            ILogger<ESignatureTemplateService> logger,
            IAuditService auditService,
            IESignatureService eSignatureService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
            _eSignatureService = eSignatureService;
        }

        public async Task<ESignatureTemplateResponse> CreateTemplateAsync(CreateESignatureTemplateRequest request, int tenantId, int userId)
        {
            try
            {
                _logger.LogInformation("Creating e-signature template for tenant {TenantId} by user {UserId}", tenantId, userId);

                var validationErrors = await GetTemplateValidationErrorsAsync(request, tenantId);
                if (validationErrors.Any())
                    throw new ArgumentException($"Template validation failed: {string.Join(", ", validationErrors)}");

                var template = new ESignatureTemplate
                {
                    TenantId = tenantId,
                    Name = request.Name,
                    Description = request.Description,
                    Category = request.Category,
                    TemplateUrl = request.TemplateUrl,
                    TemplateHash = await GenerateTemplateHashAsync(request.TemplateUrl),
                    TemplateFormat = request.TemplateFormat,
                    IsActive = request.IsActive,
                    IsPublic = request.IsPublic,
                    CreatedByUserId = userId,
                    FieldDefinitions = System.Text.Json.JsonSerializer.Serialize(request.Fields),
                    SignatureFields = GenerateSignatureFieldsConfiguration(request.Fields),
                    WorkflowConfiguration = GenerateDefaultWorkflowConfiguration(),
                    DefaultAuthenticationMethod = request.DefaultAuthenticationMethod,
                    DefaultExpiryDays = request.DefaultExpiryDays,
                    DefaultSigningInstructions = request.DefaultSigningInstructions,
                    Language = request.Language,
                    CalendarType = request.CalendarType,
                    UsageCount = 0,
                    Version = 1,
                    Tags = request.Tags,
                    Timestamp = DateTime.UtcNow
                };

                _context.ESignatureTemplates.Add(template);
                await _context.SaveChangesAsync();

                foreach (var fieldRequest in request.Fields)
                {
                    var field = new ESignatureTemplateField
                    {
                        TenantId = tenantId,
                        TemplateId = template.Id,
                        FieldName = fieldRequest.FieldName,
                        FieldType = fieldRequest.FieldType,
                        Label = fieldRequest.Label,
                        Description = fieldRequest.Description,
                        IsRequired = fieldRequest.IsRequired,
                        PageNumber = fieldRequest.PageNumber ?? 1,
                        PositionX = (double)(fieldRequest.PositionX ?? 0),
                        PositionY = (double)(fieldRequest.PositionY ?? 0),
                        Width = (double)(fieldRequest.Width ?? 0),
                        Height = (double)(fieldRequest.Height ?? 0),
                        DefaultValue = fieldRequest.DefaultValue,
                        ValidationRules = fieldRequest.ValidationRules,
                        Options = fieldRequest.Options,
                        DisplayOrder = fieldRequest.DisplayOrder,
                        AssignedRole = fieldRequest.AssignedRole,
                        Timestamp = DateTime.UtcNow
                    };

                    _context.ESignatureTemplateFields.Add(field);
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureTemplate),
                    EntityId = template.Id.ToString(),
                    Action = "Create",
                    Changes = new Dictionary<string, object?> { ["template"] = template },
                    Timestamp = DateTime.UtcNow
                });

                return await GetTemplateAsync(template.Id, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating e-signature template for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<ESignatureTemplateResponse> GetTemplateAsync(int templateId, int tenantId)
        {
            try
            {
                var template = await _context.ESignatureTemplates
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.Fields.OrderBy(f => f.DisplayOrder))
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId && !t.IsDeleted);

                if (template == null)
                    return null;

                var response = _mapper.Map<ESignatureTemplateResponse>(template);
                response.UsageStats = await GetTemplateUsageStatsAsync(templateId, tenantId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting e-signature template {TemplateId} for tenant {TenantId}", templateId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureTemplateListResponse> GetTemplatesAsync(ESignatureTemplateListRequest request, int tenantId)
        {
            try
            {
                var query = _context.ESignatureTemplates
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.Fields)
                    .Where(t => t.TenantId == tenantId && !t.IsDeleted);

                if (!string.IsNullOrEmpty(request.Category))
                    query = query.Where(t => t.Category == request.Category);

                if (request.IsActive.HasValue)
                    query = query.Where(t => t.IsActive == request.IsActive.Value);

                if (request.IsPublic.HasValue)
                    query = query.Where(t => t.IsPublic == request.IsPublic.Value);

                if (!string.IsNullOrEmpty(request.SearchTerm))
                    query = query.Where(t => t.Name.Contains(request.SearchTerm) || t.Description.Contains(request.SearchTerm));

                if (!string.IsNullOrEmpty(request.Tags))
                    query = query.Where(t => t.Tags.Contains(request.Tags));

                if (!string.IsNullOrEmpty(request.Language))
                    query = query.Where(t => t.Language == request.Language);

                if (request.CreatedByUserId.HasValue)
                    query = query.Where(t => t.CreatedByUserId == request.CreatedByUserId.Value);

                switch (request.SortBy?.ToLower())
                {
                    case "name":
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(t => t.Name)
                            : query.OrderBy(t => t.Name);
                        break;
                    case "category":
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(t => t.Category)
                            : query.OrderBy(t => t.Category);
                        break;
                    case "createdat":
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(t => t.CreatedAt)
                            : query.OrderBy(t => t.CreatedAt);
                        break;
                    case "usagecount":
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(t => t.UsageCount)
                            : query.OrderBy(t => t.UsageCount);
                        break;
                    default:
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(t => t.Name)
                            : query.OrderBy(t => t.Name);
                        break;
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var templates = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var templateResponses = new List<ESignatureTemplateResponse>();
                foreach (var template in templates)
                {
                    var response = _mapper.Map<ESignatureTemplateResponse>(template);
                    response.UsageStats = await GetTemplateUsageStatsAsync(template.Id, tenantId);
                    templateResponses.Add(response);
                }

                var categories = await _context.ESignatureTemplates
                    .Where(t => t.TenantId == tenantId && !t.IsDeleted)
                    .Select(t => t.Category)
                    .Distinct()
                    .ToListAsync();

                var tags = await _context.ESignatureTemplates
                    .Where(t => t.TenantId == tenantId && !t.IsDeleted && !string.IsNullOrEmpty(t.Tags))
                    .SelectMany(t => t.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .Select(tag => tag.Trim())
                    .Distinct()
                    .ToListAsync();

                return new ESignatureTemplateListResponse
                {
                    Templates = templateResponses,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1,
                    Categories = categories,
                    Tags = tags
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting e-signature templates for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<ESignatureDocumentResponse> CreateDocumentFromTemplateAsync(CreateDocumentFromTemplateRequest request, int tenantId, int userId)
        {
            try
            {
                _logger.LogInformation("Creating document from template {TemplateId} for tenant {TenantId}", request.TemplateId, tenantId);

                var template = await _context.ESignatureTemplates
                    .Include(t => t.Fields)
                    .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == tenantId && t.IsActive && !t.IsDeleted);

                if (template == null)
                    throw new ArgumentException("Template not found or inactive");

                var documentUrl = await GenerateDocumentFromTemplateAsync(template, request.FieldValues);

                var createDocumentRequest = new CreateESignatureDocumentRequest
                {
                    Title = request.Title,
                    Description = request.Description,
                    DocumentType = template.Category,
                    DocumentUrl = documentUrl,
                    ExpiryDate = request.ExpiryDate,
                    SigningInstructions = request.SigningInstructions ?? template.DefaultSigningInstructions,
                    RequireAllSigners = request.RequireAllSigners,
                    AllowDelegation = request.AllowDelegation,
                    AuthenticationMethod = request.AuthenticationMethod ?? template.DefaultAuthenticationMethod,
                    TemplateId = template.Id,
                    Language = request.Language,
                    CalendarType = request.CalendarType,
                    Signers = request.Signers
                };

                var document = await _eSignatureService.CreateDocumentAsync(createDocumentRequest, tenantId, userId);

                await UpdateTemplateUsageStatsAsync(template.Id, tenantId);

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document from template {TemplateId}", request.TemplateId);
                throw;
            }
        }

        public async Task<ESignatureTemplatePreviewResponse> GenerateTemplatePreviewAsync(ESignatureTemplatePreviewRequest request, int tenantId)
        {
            try
            {
                var template = await _context.ESignatureTemplates
                    .Include(t => t.Fields)
                    .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == tenantId && !t.IsDeleted);

                if (template == null)
                    throw new ArgumentException("Template not found");

                var previewUrl = await GenerateTemplatePreviewUrlAsync(template, request.FieldValues);
                var previewHtml = await GenerateTemplatePreviewHtmlAsync(template, request.FieldValues);

                var fieldPreviews = template.Fields.Select(f => new ESignatureTemplateFieldPreview
                {
                    FieldName = f.FieldName,
                    FieldType = f.FieldType,
                    Label = f.Label,
                    Value = request.FieldValues.ContainsKey(f.FieldName) ? request.FieldValues[f.FieldName] : f.DefaultValue,
                    IsRequired = f.IsRequired,
                    PageNumber = f.PageNumber,
                    PositionX = (decimal?)f.PositionX,
                    PositionY = (decimal?)f.PositionY,
                    Width = (decimal?)f.Width,
                    Height = (decimal?)f.Height,
                    AssignedRole = f.AssignedRole
                }).ToList();

                return new ESignatureTemplatePreviewResponse
                {
                    TemplateId = template.Id,
                    TemplateName = template.Name,
                    PreviewUrl = previewUrl,
                    PreviewHtml = previewHtml,
                    Fields = fieldPreviews,
                    GeneratedAt = DateTime.UtcNow,
                    Language = request.Language,
                    CalendarType = request.CalendarType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating template preview for template {TemplateId}", request.TemplateId);
                throw;
            }
        }

        private async Task<string> GenerateTemplateHashAsync(string templateUrl)
        {
            return Guid.NewGuid().ToString("N");
        }

        private string GenerateSignatureFieldsConfiguration(List<CreateESignatureTemplateFieldRequest> fields)
        {
            var signatureFields = fields.Where(f => f.FieldType == "Signature").ToList();
            return System.Text.Json.JsonSerializer.Serialize(signatureFields);
        }

        private string GenerateDefaultWorkflowConfiguration()
        {
            var defaultWorkflow = new
            {
                Type = "Sequential",
                Steps = new[]
                {
                    new { Order = 1, Type = "Sign", Name = "Primary Signature" }
                }
            };
            return System.Text.Json.JsonSerializer.Serialize(defaultWorkflow);
        }

        private async Task<string> GenerateDocumentFromTemplateAsync(ESignatureTemplate template, Dictionary<string, string> fieldValues)
        {
            return $"/documents/generated/{Guid.NewGuid()}.pdf";
        }

        private async Task<string> GenerateTemplatePreviewUrlAsync(ESignatureTemplate template, Dictionary<string, string> fieldValues)
        {
            return $"/templates/preview/{template.Id}?preview={Guid.NewGuid()}";
        }

        private async Task<string> GenerateTemplatePreviewHtmlAsync(ESignatureTemplate template, Dictionary<string, string> fieldValues)
        {
            var html = $@"
                <div class='template-preview'>
                    <h2>{template.Name}</h2>
                    <p>{template.Description}</p>
                    <div class='fields'>
                        {string.Join("", template.Fields.Select(f => $@"
                            <div class='field'>
                                <label>{f.Label}</label>
                                <span>{(fieldValues.ContainsKey(f.FieldName) ? fieldValues[f.FieldName] : f.DefaultValue ?? "[Not filled]")}</span>
                            </div>
                        "))}
                    </div>
                </div>";
            return html;
        }

        public async Task UpdateTemplateUsageStatsAsync(int templateId, int tenantId)
        {
            var template = await _context.ESignatureTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId && !t.IsDeleted);

            if (template != null)
            {
                template.UsageCount++;
                template.LastUsedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<ESignatureTemplateUsageStats> GetTemplateUsageStatsAsync(int templateId, int tenantId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var template = await _context.ESignatureTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId && !t.IsDeleted);

            if (template == null)
                return null;

            var documentsQuery = _context.ESignatureDocuments
                .Where(d => d.TemplateId == templateId && d.TenantId == tenantId && !d.IsDeleted);

            if (fromDate.HasValue)
                documentsQuery = documentsQuery.Where(d => d.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                documentsQuery = documentsQuery.Where(d => d.CreatedAt <= toDate.Value);

            var totalUsage = await documentsQuery.CountAsync();
            var activeDocuments = await documentsQuery.CountAsync(d => d.Status == "Pending" || d.Status == "InProgress");
            var completedDocuments = await documentsQuery.CountAsync(d => d.Status == "Completed");

            var completionRate = totalUsage > 0 ? (decimal)completedDocuments / totalUsage * 100 : 0;

            return new ESignatureTemplateUsageStats
            {
                TotalUsage = totalUsage,
                ActiveDocuments = activeDocuments,
                CompletedDocuments = completedDocuments,
                CompletionRate = completionRate,
                AverageCompletionTime = 0,
                LastUsed = template.LastUsedAt,
                MonthlyUsage = new List<ESignatureTemplateMonthlyUsage>()
            };
        }

        public async Task<List<string>> GetTemplateValidationErrorsAsync(CreateESignatureTemplateRequest request, int tenantId)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(request.Name))
                errors.Add("Template name is required");

            if (string.IsNullOrEmpty(request.TemplateUrl))
                errors.Add("Template URL is required");

            if (request.Fields == null || !request.Fields.Any())
                errors.Add("At least one template field is required");

            if (request.Fields != null)
            {
                var fieldNames = request.Fields.Select(f => f.FieldName).ToList();
                if (fieldNames.Count != fieldNames.Distinct().Count())
                    errors.Add("Field names must be unique");

                var signatureFields = request.Fields.Where(f => f.FieldType == "Signature").ToList();
                if (!signatureFields.Any())
                    errors.Add("At least one signature field is required");
            }

            var existingTemplate = await _context.ESignatureTemplates
                .FirstOrDefaultAsync(t => t.Name == request.Name && t.TenantId == tenantId && !t.IsDeleted);

            if (existingTemplate != null)
                errors.Add("Template name already exists");

            return errors;
        }

        public async Task<ESignatureTemplateResponse> UpdateTemplateAsync(int templateId, UpdateESignatureTemplateRequest request, int tenantId, int userId)
        {
            try
            {
                var template = await _context.ESignatureTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId && !t.IsDeleted);

                if (template == null)
                    throw new ArgumentException("Template not found");

                var oldValues = System.Text.Json.JsonSerializer.Serialize(template);

                if (!string.IsNullOrEmpty(request.Name))
                    template.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Description))
                    template.Description = request.Description;
                if (!string.IsNullOrEmpty(request.Category))
                    template.Category = request.Category;
                if (request.IsActive.HasValue)
                    template.IsActive = request.IsActive.Value;
                if (request.IsPublic.HasValue)
                    template.IsPublic = request.IsPublic.Value;
                if (!string.IsNullOrEmpty(request.DefaultAuthenticationMethod))
                    template.DefaultAuthenticationMethod = request.DefaultAuthenticationMethod;
                if (request.DefaultExpiryDays.HasValue)
                    template.DefaultExpiryDays = request.DefaultExpiryDays.Value;
                if (!string.IsNullOrEmpty(request.DefaultSigningInstructions))
                    template.DefaultSigningInstructions = request.DefaultSigningInstructions;
                if (!string.IsNullOrEmpty(request.Language))
                    template.Language = request.Language;
                if (!string.IsNullOrEmpty(request.CalendarType))
                    template.CalendarType = request.CalendarType;
                if (!string.IsNullOrEmpty(request.Tags))
                    template.Tags = request.Tags;

                template.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureTemplate),
                    EntityId = template.Id.ToString(),
                    Action = "Update",
                    Changes = new Dictionary<string, object?> { ["oldTemplate"] = oldValues },
                    Timestamp = DateTime.UtcNow
                });

                return await GetTemplateAsync(templateId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template {TemplateId} for tenant {TenantId}", templateId, tenantId);
                throw;
            }
        }

        public async Task<bool> DeleteTemplateAsync(int templateId, int tenantId, int userId)
        {
            try
            {
                var template = await _context.ESignatureTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId && !t.IsDeleted);

                if (template == null)
                    return false;

                var documentsUsingTemplate = await _context.ESignatureDocuments
                    .AnyAsync(d => d.TemplateId == templateId && d.TenantId == tenantId && !d.IsDeleted);

                if (documentsUsingTemplate)
                    throw new InvalidOperationException("Cannot delete template that is being used by documents");

                template.IsDeleted = true;
                template.DeletedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureTemplate),
                    EntityId = template.Id.ToString(),
                    Action = "Delete",
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId} for tenant {TenantId}", templateId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureTemplateFieldResponse> AddTemplateFieldAsync(int templateId, CreateESignatureTemplateFieldRequest request, int tenantId, int userId)
        {
            try
            {
                var template = await _context.ESignatureTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId && !t.IsDeleted);

                if (template == null)
                    throw new ArgumentException("Template not found");

                var field = new ESignatureTemplateField
                {
                    TenantId = tenantId,
                    TemplateId = templateId,
                    FieldName = request.FieldName,
                    FieldType = request.FieldType,
                    Label = request.Label,
                    Description = request.Description,
                    IsRequired = request.IsRequired,
                    PageNumber = request.PageNumber ?? 1,
                    PositionX = (double)(request.PositionX ?? 0),
                    PositionY = (double)(request.PositionY ?? 0),
                    Width = (double)(request.Width ?? 100),
                    Height = (double)(request.Height ?? 30),
                    DefaultValue = request.DefaultValue,
                    ValidationRules = request.ValidationRules,
                    Options = request.Options,
                    DisplayOrder = request.DisplayOrder,
                    AssignedRole = request.AssignedRole,
                    Timestamp = DateTime.UtcNow
                };

                _context.ESignatureTemplateFields.Add(field);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureTemplateField),
                    EntityId = field.Id.ToString(),
                    Action = "Create",
                    Changes = new Dictionary<string, object?> { ["field"] = field },
                    Timestamp = DateTime.UtcNow
                });

                return _mapper.Map<ESignatureTemplateFieldResponse>(field);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding field to template {TemplateId} for tenant {TenantId}", templateId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureTemplateFieldResponse> UpdateTemplateFieldAsync(int fieldId, UpdateESignatureTemplateFieldRequest request, int tenantId, int userId)
        {
            try
            {
                var field = await _context.ESignatureTemplateFields
                    .FirstOrDefaultAsync(f => f.Id == fieldId && f.TenantId == tenantId && !f.IsDeleted);

                if (field == null)
                    throw new ArgumentException("Template field not found");

                var oldValues = System.Text.Json.JsonSerializer.Serialize(field);

                if (!string.IsNullOrEmpty(request.FieldName))
                    field.FieldName = request.FieldName;
                if (!string.IsNullOrEmpty(request.FieldType))
                    field.FieldType = request.FieldType;
                if (!string.IsNullOrEmpty(request.Label))
                    field.Label = request.Label;
                if (!string.IsNullOrEmpty(request.Description))
                    field.Description = request.Description;
                if (request.IsRequired.HasValue)
                    field.IsRequired = request.IsRequired.Value;
                if (request.PageNumber.HasValue)
                    field.PageNumber = request.PageNumber.Value;
                if (request.PositionX.HasValue)
                    field.PositionX = (double)request.PositionX.Value;
                if (request.PositionY.HasValue)
                    field.PositionY = (double)request.PositionY.Value;
                if (request.Width.HasValue)
                    field.Width = (double)request.Width.Value;
                if (request.Height.HasValue)
                    field.Height = (double)request.Height.Value;
                if (!string.IsNullOrEmpty(request.DefaultValue))
                    field.DefaultValue = request.DefaultValue;
                if (!string.IsNullOrEmpty(request.ValidationRules))
                    field.ValidationRules = request.ValidationRules;
                if (!string.IsNullOrEmpty(request.Options))
                    field.Options = request.Options;
                if (request.DisplayOrder.HasValue)
                    field.DisplayOrder = request.DisplayOrder.Value;
                if (!string.IsNullOrEmpty(request.AssignedRole))
                    field.AssignedRole = request.AssignedRole;

                field.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureTemplateField),
                    EntityId = field.Id.ToString(),
                    Action = "Update",
                    Changes = new Dictionary<string, object?> { ["oldField"] = oldValues, ["field"] = field },
                    Timestamp = DateTime.UtcNow
                });

                return _mapper.Map<ESignatureTemplateFieldResponse>(field);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template field {FieldId} for tenant {TenantId}", fieldId, tenantId);
                throw;
            }
        }

        public async Task<bool> RemoveTemplateFieldAsync(int fieldId, int tenantId, int userId)
        {
            try
            {
                var field = await _context.ESignatureTemplateFields
                    .FirstOrDefaultAsync(f => f.Id == fieldId && f.TenantId == tenantId && !f.IsDeleted);

                if (field == null)
                    return false;

                field.IsDeleted = true;
                field.DeletedAt = DateTime.UtcNow;
                field.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureTemplateField),
                    EntityId = field.Id.ToString(),
                    Action = "Delete",
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing template field {FieldId} for tenant {TenantId}", fieldId, tenantId);
                throw;
            }
        }

        public async Task<List<ESignatureTemplateFieldResponse>> GetTemplateFieldsAsync(int templateId, int tenantId)
        {
            try
            {
                var fields = await _context.ESignatureTemplateFields
                    .Where(f => f.TemplateId == templateId && f.TenantId == tenantId && !f.IsDeleted)
                    .OrderBy(f => f.DisplayOrder)
                    .ToListAsync();

                return _mapper.Map<List<ESignatureTemplateFieldResponse>>(fields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template fields for template {TemplateId} for tenant {TenantId}", templateId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureTemplateFieldResponse> GetTemplateFieldAsync(int fieldId, int tenantId)
        {
            try
            {
                var field = await _context.ESignatureTemplateFields
                    .FirstOrDefaultAsync(f => f.Id == fieldId && f.TenantId == tenantId && !f.IsDeleted);

                if (field == null)
                    return null;

                return _mapper.Map<ESignatureTemplateFieldResponse>(field);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template field {FieldId} for tenant {TenantId}", fieldId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureTemplateResponse> ActivateTemplateAsync(int templateId, int tenantId, int userId)
        {
            try
            {
                var template = await _context.ESignatureTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId && !t.IsDeleted);

                if (template == null)
                    throw new ArgumentException("Template not found");

                template.IsActive = true;
                template.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureTemplate),
                    EntityId = template.Id.ToString(),
                    Action = "Activate",
                    Timestamp = DateTime.UtcNow
                });

                return await GetTemplateAsync(templateId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating template {TemplateId} for tenant {TenantId}", templateId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureTemplateResponse> DeactivateTemplateAsync(int templateId, int tenantId, int userId)
        {
            try
            {
                var template = await _context.ESignatureTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId && !t.IsDeleted);

                if (template == null)
                    throw new ArgumentException("Template not found");

                template.IsActive = false;
                template.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureTemplate),
                    EntityId = template.Id.ToString(),
                    Action = "Deactivate",
                    Timestamp = DateTime.UtcNow
                });

                return await GetTemplateAsync(templateId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating template {TemplateId} for tenant {TenantId}", templateId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureTemplateResponse> CloneTemplateAsync(int templateId, string newName, int tenantId, int userId)
        {
            try
            {
                var originalTemplate = await _context.ESignatureTemplates
                    .Include(t => t.Fields)
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId && !t.IsDeleted);

                if (originalTemplate == null)
                    throw new ArgumentException("Template not found");

                var clonedTemplate = new ESignatureTemplate
                {
                    TenantId = tenantId,
                    Name = newName,
                    Description = $"Cloned from {originalTemplate.Name}",
                    Category = originalTemplate.Category,
                    TemplateUrl = originalTemplate.TemplateUrl,
                    TemplateHash = await GenerateTemplateHashAsync(originalTemplate.TemplateUrl),
                    TemplateFormat = originalTemplate.TemplateFormat,
                    IsActive = false,
                    IsPublic = originalTemplate.IsPublic,
                    CreatedByUserId = userId,
                    FieldDefinitions = originalTemplate.FieldDefinitions,
                    SignatureFields = originalTemplate.SignatureFields,
                    WorkflowConfiguration = originalTemplate.WorkflowConfiguration,
                    DefaultAuthenticationMethod = originalTemplate.DefaultAuthenticationMethod,
                    DefaultExpiryDays = originalTemplate.DefaultExpiryDays,
                    DefaultSigningInstructions = originalTemplate.DefaultSigningInstructions,
                    Language = originalTemplate.Language,
                    CalendarType = originalTemplate.CalendarType,
                    UsageCount = 0,
                    Version = 1,
                    Tags = originalTemplate.Tags,
                    Timestamp = DateTime.UtcNow
                };

                _context.ESignatureTemplates.Add(clonedTemplate);
                await _context.SaveChangesAsync();

                foreach (var originalField in originalTemplate.Fields)
                {
                    var clonedField = new ESignatureTemplateField
                    {
                        TenantId = tenantId,
                        TemplateId = clonedTemplate.Id,
                        FieldName = originalField.FieldName,
                        FieldType = originalField.FieldType,
                        Label = originalField.Label,
                        Description = originalField.Description,
                        IsRequired = originalField.IsRequired,
                        PageNumber = originalField.PageNumber,
                        PositionX = originalField.PositionX,
                        PositionY = originalField.PositionY,
                        Width = originalField.Width,
                        Height = originalField.Height,
                        DefaultValue = originalField.DefaultValue,
                        ValidationRules = originalField.ValidationRules,
                        Options = originalField.Options,
                        DisplayOrder = originalField.DisplayOrder,
                        AssignedRole = originalField.AssignedRole,
                        Timestamp = DateTime.UtcNow
                    };

                    _context.ESignatureTemplateFields.Add(clonedField);
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureTemplate),
                    EntityId = clonedTemplate.Id.ToString(),
                    Action = "Clone",
                    Changes = new Dictionary<string, object?> { ["OriginalTemplateId"] = templateId, ["NewName"] = newName },
                    Timestamp = DateTime.UtcNow
                });

                return await GetTemplateAsync(clonedTemplate.Id, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning template {TemplateId} for tenant {TenantId}", templateId, tenantId);
                throw;
            }
        }

        public async Task<bool> ValidateTemplateDefinitionAsync(CreateESignatureTemplateRequest request, int tenantId)
        {
            try
            {
                var errors = await GetTemplateValidationErrorsAsync(request, tenantId);
                return !errors.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template definition for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<List<string>> GetTemplateCategoriesAsync(int tenantId)
        {
            try
            {
                var categories = await _context.ESignatureTemplates
                    .Where(t => t.TenantId == tenantId && !t.IsDeleted)
                    .Select(t => t.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template categories for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public Task<List<string>> GetTemplateTagsAsync(int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ExportTemplateAsync(int templateId, string format, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<ESignatureTemplateResponse> ImportTemplateAsync(byte[] templateData, string format, int tenantId, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateTemplateFieldsAsync(int templateId, Dictionary<string, string> fieldValues, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, object>> GetTemplateMetricsAsync(int templateId, int tenantId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            throw new NotImplementedException();
        }

        public Task ProcessTemplateMaintenanceAsync(int tenantId)
        {
            throw new NotImplementedException();
        }
    }
}
