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
    public class ESignatureService : IESignatureService
    {
        private readonly NexoraDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ESignatureService> _logger;
        private readonly IESignatureAuthenticationService _authenticationService;
        private readonly IESignatureComplianceService _complianceService;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;

        public ESignatureService(
            NexoraDbContext context,
            IMapper mapper,
            ILogger<ESignatureService> logger,
            IESignatureAuthenticationService authenticationService,
            IESignatureComplianceService complianceService,
            INotificationService notificationService,
            IAuditService auditService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _authenticationService = authenticationService;
            _complianceService = complianceService;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        public async Task<ESignatureDocumentResponse> CreateDocumentAsync(CreateESignatureDocumentRequest request, int tenantId, int userId)
        {
            try
            {
                _logger.LogInformation("Creating e-signature document for tenant {TenantId} by user {UserId}", tenantId, userId);

                var document = new ESignatureDocument
                {
                    TenantId = tenantId,
                    Title = request.Title,
                    Description = request.Description,
                    DocumentType = request.DocumentType,
                    DocumentUrl = request.DocumentUrl,
                    DocumentHash = await GenerateDocumentHashAsync(request.DocumentUrl),
                    DocumentSize = await GetDocumentSizeAsync(request.DocumentUrl),
                    DocumentFormat = GetDocumentFormat(request.DocumentUrl),
                    WorkflowId = request.WorkflowId ?? await GetDefaultWorkflowIdAsync(tenantId),
                    CreatedByUserId = userId,
                    ExpiryDate = request.ExpiryDate,
                    SigningInstructions = request.SigningInstructions,
                    RequireAllSigners = request.RequireAllSigners,
                    AllowDelegation = request.AllowDelegation,
                    AuthenticationMethod = request.AuthenticationMethod,
                    TemplateId = request.TemplateId,
                    Language = request.Language,
                    CalendarType = request.CalendarType,
                    Status = "Draft",
                    Timestamp = DateTime.UtcNow
                };

                _context.ESignatureDocuments.Add(document);
                await _context.SaveChangesAsync();

                foreach (var signerRequest in request.Signers)
                {
                    var signer = new ESignatureSigner
                    {
                        TenantId = tenantId,
                        DocumentId = document.Id,
                        FullName = signerRequest.FullName,
                        Email = signerRequest.Email,
                        PhoneNumber = signerRequest.PhoneNumber,
                        NationalId = signerRequest.NationalId,
                        SignerType = signerRequest.SignerType,
                        Status = "Pending",
                        SigningOrder = signerRequest.SigningOrder,
                        IsRequired = signerRequest.IsRequired,
                        AllowDelegation = signerRequest.AllowDelegation,
                        AuthenticationMethod = signerRequest.AuthenticationMethod ?? document.AuthenticationMethod,
                        SigningInstructions = signerRequest.SigningInstructions,
                        EmailNotificationsEnabled = signerRequest.EmailNotificationsEnabled,
                        SmsNotificationsEnabled = signerRequest.SmsNotificationsEnabled,
                        SigningToken = GenerateSigningToken(),
                        TokenExpiryDate = DateTime.UtcNow.AddDays(30),
                        Timestamp = DateTime.UtcNow
                    };

                    _context.ESignatureSigners.Add(signer);
                }

                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureDocument),
                    EntityId = document.Id.ToString(),
                    Action = "Create",
                    Changes = new Dictionary<string, object?> { ["Document"] = document },
                    Timestamp = DateTime.UtcNow
                });

                return await GetDocumentAsync(document.Id, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating e-signature document for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<ESignatureDocumentResponse> UpdateDocumentAsync(int documentId, UpdateESignatureDocumentRequest request, int tenantId, int userId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    throw new ArgumentException("Document not found");

                if (document.Status != "Draft")
                    throw new InvalidOperationException("Only draft documents can be updated");

                var oldValues = System.Text.Json.JsonSerializer.Serialize(document);

                if (!string.IsNullOrEmpty(request.Title))
                    document.Title = request.Title;
                if (!string.IsNullOrEmpty(request.Description))
                    document.Description = request.Description;
                if (request.ExpiryDate.HasValue)
                    document.ExpiryDate = request.ExpiryDate;
                if (!string.IsNullOrEmpty(request.SigningInstructions))
                    document.SigningInstructions = request.SigningInstructions;
                if (request.RequireAllSigners.HasValue)
                    document.RequireAllSigners = request.RequireAllSigners.Value;
                if (request.AllowDelegation.HasValue)
                    document.AllowDelegation = request.AllowDelegation.Value;
                if (!string.IsNullOrEmpty(request.AuthenticationMethod))
                    document.AuthenticationMethod = request.AuthenticationMethod;
                if (!string.IsNullOrEmpty(request.Language))
                    document.Language = request.Language;
                if (!string.IsNullOrEmpty(request.CalendarType))
                    document.CalendarType = request.CalendarType;

                document.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureDocument),
                    EntityId = document.Id.ToString(),
                    Action = "Update",
                    Changes = new Dictionary<string, object?> { ["OldDocument"] = oldValues, ["Document"] = document },
                    Timestamp = DateTime.UtcNow
                });

                return await GetDocumentAsync(documentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating e-signature document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureDocumentResponse> GetDocumentAsync(int documentId, int tenantId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .Include(d => d.Workflow)
                    .Include(d => d.CreatedByUser)
                    .Include(d => d.Template)
                    .Include(d => d.Signers)
                    .Include(d => d.Signatures)
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    return null;

                var response = _mapper.Map<ESignatureDocumentResponse>(document);
                response.Progress = await CalculateDocumentProgressAsync(documentId, tenantId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting e-signature document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureListResponse> GetDocumentsAsync(ESignatureListRequest request, int tenantId)
        {
            try
            {
                var query = _context.ESignatureDocuments
                    .Include(d => d.Workflow)
                    .Include(d => d.CreatedByUser)
                    .Include(d => d.Template)
                    .Where(d => d.TenantId == tenantId && !d.IsDeleted);

                if (!string.IsNullOrEmpty(request.Status))
                    query = query.Where(d => d.Status == request.Status);

                if (!string.IsNullOrEmpty(request.DocumentType))
                    query = query.Where(d => d.DocumentType == request.DocumentType);

                if (request.CreatedFrom.HasValue)
                    query = query.Where(d => d.CreatedAt >= request.CreatedFrom.Value);

                if (request.CreatedTo.HasValue)
                    query = query.Where(d => d.CreatedAt <= request.CreatedTo.Value);

                if (request.ExpiryFrom.HasValue)
                    query = query.Where(d => d.ExpiryDate >= request.ExpiryFrom.Value);

                if (request.ExpiryTo.HasValue)
                    query = query.Where(d => d.ExpiryDate <= request.ExpiryTo.Value);

                if (!string.IsNullOrEmpty(request.SearchTerm))
                    query = query.Where(d => d.Title.Contains(request.SearchTerm) || d.Description.Contains(request.SearchTerm));

                if (request.IsTemplate.HasValue)
                    query = query.Where(d => d.IsTemplate == request.IsTemplate.Value);

                if (request.CreatedByUserId.HasValue)
                    query = query.Where(d => d.CreatedByUserId == request.CreatedByUserId.Value);

                if (!string.IsNullOrEmpty(request.Language))
                    query = query.Where(d => d.Language == request.Language);

                switch (request.SortBy?.ToLower())
                {
                    case "title":
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(d => d.Title)
                            : query.OrderBy(d => d.Title);
                        break;
                    case "status":
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(d => d.Status)
                            : query.OrderBy(d => d.Status);
                        break;
                    case "updatedat":
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(d => d.UpdatedAt)
                            : query.OrderBy(d => d.UpdatedAt);
                        break;
                    case "expirydate":
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(d => d.ExpiryDate)
                            : query.OrderBy(d => d.ExpiryDate);
                        break;
                    default:
                        query = request.SortDirection?.ToLower() == "desc" 
                            ? query.OrderByDescending(d => d.CreatedAt)
                            : query.OrderBy(d => d.CreatedAt);
                        break;
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var documents = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var documentResponses = new List<ESignatureDocumentResponse>();
                foreach (var document in documents)
                {
                    var response = _mapper.Map<ESignatureDocumentResponse>(document);
                    response.Progress = await CalculateDocumentProgressAsync(document.Id, tenantId);
                    documentResponses.Add(response);
                }

                return new ESignatureListResponse
                {
                    Documents = documentResponses,
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
                _logger.LogError(ex, "Error getting e-signature documents for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int documentId, int tenantId, int userId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    return false;

                if (document.Status == "InProgress" || document.Status == "Completed")
                    throw new InvalidOperationException("Cannot delete documents that are in progress or completed");

                document.IsDeleted = true;
                document.DeletedAt = DateTime.UtcNow;
                document.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureDocument),
                    EntityId = document.Id.ToString(),
                    Action = "Delete",
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting e-signature document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureDocumentResponse> SendDocumentForSigningAsync(SendDocumentForSigningRequest request, int tenantId, int userId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .Include(d => d.Signers)
                    .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    throw new ArgumentException("Document not found");

                if (document.Status != "Draft")
                    throw new InvalidOperationException("Only draft documents can be sent for signing");

                var complianceResult = await _complianceService.ValidateDocumentComplianceAsync(document.Id, tenantId);
                if (!complianceResult.IsCompliant)
                    throw new InvalidOperationException($"Document does not meet compliance requirements: {string.Join(", ", complianceResult.Violations)}");

                document.Status = "Pending";
                document.UpdatedAt = DateTime.UtcNow;

                var signersToNotify = request.SignerIds.Any() 
                    ? document.Signers.Where(s => request.SignerIds.Contains(s.Id)).ToList()
                    : document.Signers.Where(s => s.SigningOrder == 1).ToList();

                foreach (var signer in signersToNotify)
                {
                    signer.Status = "Invited";
                    signer.InvitedAt = DateTime.UtcNow;
                    signer.UpdatedAt = DateTime.UtcNow;

                    if (request.SendImmediately)
                    {
                        await SendSigningInvitationAsync(signer, request.Message, tenantId);
                    }
                    else if (request.ScheduledSendTime.HasValue)
                    {
                        await ScheduleSigningInvitationAsync(signer, request.Message, request.ScheduledSendTime.Value, tenantId);
                    }
                }

                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureDocument),
                    EntityId = document.Id.ToString(),
                    Action = "SendForSigning",
                    Changes = new Dictionary<string, object?> { ["SignerIds"] = request.SignerIds, ["Message"] = request.Message },
                    Timestamp = DateTime.UtcNow
                });

                return await GetDocumentAsync(document.Id, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending document {DocumentId} for signing for tenant {TenantId}", request.DocumentId, tenantId);
                throw;
            }
        }

        private async Task<string> GenerateDocumentHashAsync(string documentUrl)
        {
            return Guid.NewGuid().ToString("N");
        }

        private async Task<long> GetDocumentSizeAsync(string documentUrl)
        {
            return 1024; // Placeholder
        }

        private string GetDocumentFormat(string documentUrl)
        {
            return System.IO.Path.GetExtension(documentUrl)?.TrimStart('.').ToUpper() ?? "PDF";
        }

        private async Task<int> GetDefaultWorkflowIdAsync(int tenantId)
        {
            var defaultWorkflow = await _context.ESignatureWorkflows
                .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.IsDefault && w.IsActive && !w.IsDeleted);
            
            return defaultWorkflow?.Id ?? 1; // Fallback to a default workflow
        }

        private string GenerateSigningToken()
        {
            return Guid.NewGuid().ToString("N");
        }

        private async Task<ESignatureProgressResponse> CalculateDocumentProgressAsync(int documentId, int tenantId)
        {
            var signers = await _context.ESignatureSigners
                .Where(s => s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted)
                .ToListAsync();

            var totalSigners = signers.Count;
            var signedCount = signers.Count(s => s.Status == "Signed");
            var pendingCount = signers.Count(s => s.Status == "Pending" || s.Status == "Invited" || s.Status == "Viewed");
            var declinedCount = signers.Count(s => s.Status == "Declined");

            var completionPercentage = totalSigners > 0 ? (decimal)signedCount / totalSigners * 100 : 0;

            return new ESignatureProgressResponse
            {
                TotalSigners = totalSigners,
                SignedCount = signedCount,
                PendingCount = pendingCount,
                DeclinedCount = declinedCount,
                CompletionPercentage = completionPercentage,
                IsCompleted = signedCount == totalSigners,
                IsExpired = false // Would check expiry logic here
            };
        }

        private async Task SendSigningInvitationAsync(ESignatureSigner signer, string message, int tenantId)
        {
            await _notificationService.SendNotificationAsync(new NotificationRequest
            {
                TenantId = tenantId,
                RecipientEmail = signer.Email,
                Subject = "Document Signing Request",
                Message = message ?? "You have been invited to sign a document.",
                NotificationType = "Email"
            });
        }

        private async Task ScheduleSigningInvitationAsync(ESignatureSigner signer, string message, DateTime scheduledTime, int tenantId)
        {
        }

        public async Task<ESignatureDocumentResponse> CancelDocumentAsync(int documentId, string reason, int tenantId, int userId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    throw new ArgumentException("Document not found");

                if (document.Status == "Completed" || document.Status == "Cancelled")
                    throw new InvalidOperationException("Cannot cancel completed or already cancelled documents");

                document.Status = "Cancelled";
                document.UpdatedAt = DateTime.UtcNow;

                var signers = await _context.ESignatureSigners
                    .Where(s => s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted)
                    .ToListAsync();

                foreach (var signer in signers.Where(s => s.Status != "Signed"))
                {
                    signer.Status = "Cancelled";
                    signer.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureDocument),
                    EntityId = document.Id.ToString(),
                    Action = "Cancel",
                    Changes = new Dictionary<string, object?> { ["Reason"] = reason },
                    Timestamp = DateTime.UtcNow
                });

                return await GetDocumentAsync(documentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureDocumentResponse> ExpireDocumentAsync(int documentId, int tenantId, int userId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    throw new ArgumentException("Document not found");

                document.Status = "Expired";
                document.UpdatedAt = DateTime.UtcNow;

                var signers = await _context.ESignatureSigners
                    .Where(s => s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted)
                    .ToListAsync();

                foreach (var signer in signers.Where(s => s.Status != "Signed"))
                {
                    signer.Status = "Expired";
                    signer.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureDocument),
                    EntityId = document.Id.ToString(),
                    Action = "Expire",
                    Timestamp = DateTime.UtcNow
                });

                return await GetDocumentAsync(documentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureSignerResponse> AddSignerAsync(int documentId, CreateESignatureSignerRequest request, int tenantId, int userId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    throw new ArgumentException("Document not found");

                if (document.Status != "Draft")
                    throw new InvalidOperationException("Can only add signers to draft documents");

                var signer = new ESignatureSigner
                {
                    TenantId = tenantId,
                    DocumentId = documentId,
                    FullName = request.FullName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    NationalId = request.NationalId,
                    SignerType = request.SignerType,
                    Status = "Pending",
                    SigningOrder = request.SigningOrder,
                    IsRequired = request.IsRequired,
                    AllowDelegation = request.AllowDelegation,
                    AuthenticationMethod = request.AuthenticationMethod ?? document.AuthenticationMethod,
                    SigningInstructions = request.SigningInstructions,
                    EmailNotificationsEnabled = request.EmailNotificationsEnabled,
                    SmsNotificationsEnabled = request.SmsNotificationsEnabled,
                    SigningToken = GenerateSigningToken(),
                    TokenExpiryDate = DateTime.UtcNow.AddDays(30),
                    Timestamp = DateTime.UtcNow
                };

                _context.ESignatureSigners.Add(signer);
                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureSigner),
                    EntityId = signer.Id.ToString(),
                    Action = "Create",
                    Changes = new Dictionary<string, object?> { ["Signer"] = signer },
                    Timestamp = DateTime.UtcNow
                });

                return _mapper.Map<ESignatureSignerResponse>(signer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding signer to document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureSignerResponse> UpdateSignerAsync(int signerId, UpdateESignatureSignerRequest request, int tenantId, int userId)
        {
            try
            {
                var signer = await _context.ESignatureSigners
                    .Include(s => s.Document)
                    .FirstOrDefaultAsync(s => s.Id == signerId && s.TenantId == tenantId && !s.IsDeleted);

                if (signer == null)
                    throw new ArgumentException("Signer not found");

                if (signer.Document.Status != "Draft")
                    throw new InvalidOperationException("Can only update signers for draft documents");

                var oldValues = System.Text.Json.JsonSerializer.Serialize(signer);

                if (!string.IsNullOrEmpty(request.FullName))
                    signer.FullName = request.FullName;
                if (!string.IsNullOrEmpty(request.Email))
                    signer.Email = request.Email;
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                    signer.PhoneNumber = request.PhoneNumber;
                if (!string.IsNullOrEmpty(request.NationalId))
                    signer.NationalId = request.NationalId;
                if (!string.IsNullOrEmpty(request.SignerType))
                    signer.SignerType = request.SignerType;
                if (request.SigningOrder.HasValue)
                    signer.SigningOrder = request.SigningOrder.Value;
                if (request.IsRequired.HasValue)
                    signer.IsRequired = request.IsRequired.Value;
                if (request.AllowDelegation.HasValue)
                    signer.AllowDelegation = request.AllowDelegation.Value;
                if (!string.IsNullOrEmpty(request.AuthenticationMethod))
                    signer.AuthenticationMethod = request.AuthenticationMethod;
                if (!string.IsNullOrEmpty(request.SigningInstructions))
                    signer.SigningInstructions = request.SigningInstructions;
                if (request.EmailNotificationsEnabled.HasValue)
                    signer.EmailNotificationsEnabled = request.EmailNotificationsEnabled.Value;
                if (request.SmsNotificationsEnabled.HasValue)
                    signer.SmsNotificationsEnabled = request.SmsNotificationsEnabled.Value;

                signer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureSigner),
                    EntityId = signer.Id.ToString(),
                    Action = "Update",

                    Changes = new Dictionary<string, object?> { ["Signer"] = signer },
                    Timestamp = DateTime.UtcNow
                });

                return _mapper.Map<ESignatureSignerResponse>(signer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating signer {SignerId} for tenant {TenantId}", signerId, tenantId);
                throw;
            }
        }

        public async Task<bool> RemoveSignerAsync(int signerId, int tenantId, int userId)
        {
            try
            {
                var signer = await _context.ESignatureSigners
                    .Include(s => s.Document)
                    .FirstOrDefaultAsync(s => s.Id == signerId && s.TenantId == tenantId && !s.IsDeleted);

                if (signer == null)
                    return false;

                if (signer.Document.Status != "Draft")
                    throw new InvalidOperationException("Can only remove signers from draft documents");

                if (signer.Status == "Signed")
                    throw new InvalidOperationException("Cannot remove signers who have already signed");

                signer.IsDeleted = true;
                signer.DeletedAt = DateTime.UtcNow;
                signer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureSigner),
                    EntityId = signer.Id.ToString(),
                    Action = "Delete",
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing signer {SignerId} for tenant {TenantId}", signerId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureSignerResponse> GetSignerAsync(int signerId, int tenantId)
        {
            try
            {
                var signer = await _context.ESignatureSigners
                    .Include(s => s.Document)
                    .Include(s => s.DelegatedToUser)
                    .FirstOrDefaultAsync(s => s.Id == signerId && s.TenantId == tenantId && !s.IsDeleted);

                if (signer == null)
                    return null;

                return _mapper.Map<ESignatureSignerResponse>(signer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signer {SignerId} for tenant {TenantId}", signerId, tenantId);
                throw;
            }
        }

        public async Task<List<ESignatureSignerResponse>> GetDocumentSignersAsync(int documentId, int tenantId)
        {
            try
            {
                var signers = await _context.ESignatureSigners
                    .Include(s => s.DelegatedToUser)
                    .Where(s => s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted)
                    .OrderBy(s => s.SigningOrder)
                    .ToListAsync();

                return _mapper.Map<List<ESignatureSignerResponse>>(signers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signers for document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureSignatureResponse> CreateSignatureAsync(CreateSignatureRequest request, int tenantId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    throw new ArgumentException("Document not found");

                var signer = await _context.ESignatureSigners
                    .FirstOrDefaultAsync(s => s.Id == request.SignerId && s.TenantId == tenantId && !s.IsDeleted);

                if (signer == null)
                    throw new ArgumentException("Signer not found");

                if (signer.Status == "Signed")
                    throw new InvalidOperationException("Signer has already signed this document");

                var authResult = await _authenticationService.ValidateSignerAuthenticationAsync(
                    request.SignerId, CancellationToken.None);

                if (!authResult)
                    throw new UnauthorizedAccessException("Authentication failed: Signer authentication is invalid");

                var signature = new ESignatureSignature
                {
                    TenantId = tenantId,
                    DocumentId = request.DocumentId,
                    SignerId = request.SignerId,
                    SignatureType = request.SignatureType,
                    SignatureData = request.SignatureData,
                    SignatureHash = await GenerateSignatureHashAsync(request.SignatureData),
                    CertificateData = request.CertificateData,
                    CertificateFingerprint = await GenerateCertificateFingerprintAsync(request.CertificateData),
                    SignedAt = DateTime.UtcNow,
                    IpAddress = GetClientIpAddress(),
                    AuthenticationMethod = request.AuthenticationMethod,
                    AuthenticationReference = request.AuthenticationReference,
                    IsValid = true,
                    ValidatedAt = DateTime.UtcNow,
                    ValidationReference = Guid.NewGuid().ToString("N"),
                    SignatureFormat = "PKCS#7",
                    PageNumber = request.PageNumber,
                    PositionX = request.PositionX,
                    PositionY = request.PositionY,
                    Width = request.Width,
                    Height = request.Height,
                    SigningReason = request.SigningReason,
                    SigningLocation = request.SigningLocation,
                    BiometricData = request.BiometricData,
                    DeviceInformation = request.DeviceInformation,
                    Timestamp = DateTime.UtcNow
                };

                _context.ESignatureSignatures.Add(signature);

                signer.Status = "Signed";
                signer.SignedAt = DateTime.UtcNow;
                signer.UpdatedAt = DateTime.UtcNow;

                await CheckDocumentCompletionAsync(document.Id, tenantId);

                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = signer.Id.ToString(),
                    EntityName = nameof(ESignatureSignature),
                    EntityId = signature.Id.ToString(),
                    Action = "Create",
                    Changes = new Dictionary<string, object?> { ["signature"] = signature },
                    Timestamp = DateTime.UtcNow
                });

                return _mapper.Map<ESignatureSignatureResponse>(signature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating signature for document {DocumentId} signer {SignerId} for tenant {TenantId}", 
                    request.DocumentId, request.SignerId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureSignatureResponse> GetSignatureAsync(int signatureId, int tenantId)
        {
            try
            {
                var signature = await _context.ESignatureSignatures
                    .Include(s => s.Signer)
                    .FirstOrDefaultAsync(s => s.Id == signatureId && s.TenantId == tenantId);

                if (signature == null)
                    return null;

                return _mapper.Map<ESignatureSignatureResponse>(signature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signature {SignatureId} for tenant {TenantId}", signatureId, tenantId);
                throw;
            }
        }

        public async Task<List<ESignatureSignatureResponse>> GetDocumentSignaturesAsync(int documentId, int tenantId)
        {
            try
            {
                var signatures = await _context.ESignatureSignatures
                    .Include(s => s.Signer)
                    .Where(s => s.DocumentId == documentId && s.TenantId == tenantId)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();

                return _mapper.Map<List<ESignatureSignatureResponse>>(signatures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signatures for document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<bool> ValidateSignatureAsync(int signatureId, int tenantId)
        {
            try
            {
                var signature = await _context.ESignatureSignatures
                    .FirstOrDefaultAsync(s => s.Id == signatureId && s.TenantId == tenantId);

                if (signature == null)
                    return false;

                var isValid = await _complianceService.ValidateSignatureIntegrityAsync(signature.Id, CancellationToken.None);
                
                signature.IsValid = isValid;
                signature.ValidatedAt = DateTime.UtcNow;
                signature.ValidationReference = Guid.NewGuid().ToString("N");

                await _context.SaveChangesAsync();

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating signature {SignatureId} for tenant {TenantId}", signatureId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureDocumentResponse> DeclineSigningAsync(DeclineSigningRequest request, int tenantId)
        {
            try
            {
                var signer = await _context.ESignatureSigners
                    .Include(s => s.Document)
                    .FirstOrDefaultAsync(s => s.Id == request.SignerId && s.TenantId == tenantId && !s.IsDeleted);

                if (signer == null)
                    throw new ArgumentException("Signer not found");

                if (signer.Status == "Signed")
                    throw new InvalidOperationException("Cannot decline after signing");

                signer.Status = "Declined";
                signer.DeclinedAt = DateTime.UtcNow;
                signer.DeclineReason = request.Reason;
                signer.UpdatedAt = DateTime.UtcNow;

                await CheckDocumentCompletionAsync(signer.DocumentId, tenantId);

                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = signer.Id.ToString(),
                    EntityName = nameof(ESignatureSigner),
                    EntityId = signer.Id.ToString(),
                    Action = "Decline",
                    Changes = new Dictionary<string, object?> { ["Reason"] = request.Reason },
                    Timestamp = DateTime.UtcNow
                });

                return await GetDocumentAsync(request.DocumentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining signature for document {DocumentId} signer {SignerId} for tenant {TenantId}", 
                    request.DocumentId, request.SignerId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureDocumentResponse> DelegateSigningAsync(DelegateSigningRequest request, int tenantId, int userId)
        {
            try
            {
                var signer = await _context.ESignatureSigners
                    .Include(s => s.Document)
                    .FirstOrDefaultAsync(s => s.Id == request.SignerId && s.TenantId == tenantId && !s.IsDeleted);

                if (signer == null)
                    throw new ArgumentException("Signer not found");

                if (!signer.AllowDelegation)
                    throw new InvalidOperationException("Delegation is not allowed for this signer");

                if (signer.Status == "Signed")
                    throw new InvalidOperationException("Cannot delegate after signing");

                var delegatedToUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.DelegatedToUserId && u.TenantId == tenantId && !u.IsDeleted);

                if (delegatedToUser == null)
                    throw new ArgumentException("Delegated user not found");

                signer.Status = "Delegated";
                signer.DelegatedToUserId = request.DelegatedToUserId;
                signer.DelegatedAt = DateTime.UtcNow;
                signer.DelegationReason = request.Reason;
                signer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = userId.ToString(),
                    EntityName = nameof(ESignatureSigner),
                    EntityId = signer.Id.ToString(),
                    Action = "Delegate",
                    Changes = new Dictionary<string, object?> { 
                        ["DelegatedToUserId"] = request.DelegatedToUserId, 
                        ["Reason"] = request.Reason 
                    },
                    Timestamp = DateTime.UtcNow
                });

                return await GetDocumentAsync(request.DocumentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delegating signature for document {DocumentId} signer {SignerId} for tenant {TenantId}", 
                    request.DocumentId, request.SignerId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureProgressResponse> GetDocumentProgressAsync(int documentId, int tenantId)
        {
            try
            {
                return await CalculateDocumentProgressAsync(documentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document progress for {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<ESignatureStatisticsResponse> GetStatisticsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.ESignatureDocuments
                    .Where(d => d.TenantId == tenantId && !d.IsDeleted);

                if (fromDate.HasValue)
                    query = query.Where(d => d.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(d => d.CreatedAt <= toDate.Value);

                var documents = await query.ToListAsync();

                var totalDocuments = documents.Count;
                var completedDocuments = documents.Count(d => d.Status == "Completed");
                var pendingDocuments = documents.Count(d => d.Status == "Pending" || d.Status == "InProgress");
                var expiredDocuments = documents.Count(d => d.Status == "Expired");
                var cancelledDocuments = documents.Count(d => d.Status == "Cancelled");

                var completionRate = totalDocuments > 0 ? (decimal)completedDocuments / totalDocuments * 100 : 0;

                var signatures = await _context.ESignatureSignatures
                    .Where(s => s.TenantId == tenantId && documents.Select(d => d.Id).Contains(s.DocumentId))
                    .ToListAsync();

                var signers = await _context.ESignatureSigners
                    .Where(s => s.TenantId == tenantId && documents.Select(d => d.Id).Contains(s.DocumentId) && !s.IsDeleted)
                    .ToListAsync();

                return new ESignatureStatisticsResponse
                {
                    TotalDocuments = totalDocuments,
                    CompletedDocuments = completedDocuments,
                    PendingDocuments = pendingDocuments,
                    ExpiredDocuments = expiredDocuments,
                    CancelledDocuments = cancelledDocuments,
                    CompletionRate = completionRate,
                    AverageCompletionTime = CalculateAverageCompletionTime(documents.Where(d => d.Status == "Completed")),
                    TotalSignatures = signatures.Count,
                    TotalSigners = signers.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<byte[]> GenerateDocumentPdfAsync(int documentId, int tenantId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .Include(d => d.Signatures)
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    throw new ArgumentException("Document not found");

                var pdfBytes = await _complianceService.GenerateSignedDocumentPdfAsync(documentId, CancellationToken.None);
                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<byte[]> GenerateLegalEvidencePackageAsync(int documentId, int tenantId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .Include(d => d.Signatures)
                    .Include(d => d.AuditLogs)
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    throw new ArgumentException("Document not found");

                var evidencePackage = await _complianceService.GenerateLegalEvidencePackageAsync(documentId, tenantId);
                return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(evidencePackage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating legal evidence package for document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<string> GenerateSigningLinkAsync(int documentId, int signerId, int tenantId)
        {
            try
            {
                var signer = await _context.ESignatureSigners
                    .Include(s => s.Document)
                    .FirstOrDefaultAsync(s => s.Id == signerId && s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted);

                if (signer == null)
                    throw new ArgumentException("Signer not found");

                if (signer.Document.Status != "Pending" && signer.Document.Status != "InProgress")
                    throw new InvalidOperationException("Document is not available for signing");

                var token = GenerateSigningToken();
                signer.SigningToken = token;
                signer.TokenExpiryDate = DateTime.UtcNow.AddDays(30);
                signer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return $"https://app.nexora.sa/esignature/sign/{documentId}/{signerId}?token={token}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating signing link for document {DocumentId} signer {SignerId} for tenant {TenantId}", 
                    documentId, signerId, tenantId);
                throw;
            }
        }

        public async Task<bool> ValidateDocumentIntegrityAsync(int documentId, int tenantId)
        {
            try
            {
                var document = await _context.ESignatureDocuments
                    .Include(d => d.Signatures)
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    return false;

                var validationResult = await _complianceService.ValidateDocumentIntegrityAsync(documentId, tenantId);
                return validationResult.IsValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating document integrity for {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<List<ESignatureAuditLog>> GetDocumentAuditTrailAsync(int documentId, int tenantId)
        {
            try
            {
                return await _context.ESignatureAuditLogs
                    .Where(a => a.DocumentId == documentId && a.TenantId == tenantId)
                    .OrderBy(a => a.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit trail for document {DocumentId} for tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task SendReminderNotificationsAsync(int tenantId)
        {
            try
            {
                var pendingSigners = await _context.ESignatureSigners
                    .Include(s => s.Document)
                    .Where(s => s.TenantId == tenantId && 
                               (s.Status == "Invited" || s.Status == "Viewed") &&
                               s.Document.Status == "Pending" &&
                               !s.IsDeleted && !s.Document.IsDeleted)
                    .ToListAsync();

                foreach (var signer in pendingSigners)
                {
                    var daysSinceInvited = (DateTime.UtcNow - signer.InvitedAt.GetValueOrDefault()).TotalDays;
                    
                    if (daysSinceInvited >= 3 && daysSinceInvited % 3 == 0)
                    {
                        await SendSigningReminderAsync(signer, tenantId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminder notifications for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task ProcessExpiredDocumentsAsync(int tenantId)
        {
            try
            {
                var expiredDocuments = await _context.ESignatureDocuments
                    .Where(d => d.TenantId == tenantId && 
                               d.ExpiryDate.HasValue && 
                               d.ExpiryDate.Value < DateTime.UtcNow &&
                               d.Status != "Completed" && 
                               d.Status != "Expired" && 
                               d.Status != "Cancelled" &&
                               !d.IsDeleted)
                    .ToListAsync();

                foreach (var document in expiredDocuments)
                {
                    await ExpireDocumentAsync(document.Id, tenantId, 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired documents for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> VerifySignerAccessAsync(int documentId, int signerId, string token, int tenantId)
        {
            try
            {
                var signer = await _context.ESignatureSigners
                    .Include(s => s.Document)
                    .FirstOrDefaultAsync(s => s.Id == signerId && 
                                            s.DocumentId == documentId && 
                                            s.TenantId == tenantId && 
                                            !s.IsDeleted);

                if (signer == null)
                    return false;

                if (signer.SigningToken != token)
                    return false;

                if (signer.TokenExpiryDate.HasValue && signer.TokenExpiryDate.Value < DateTime.UtcNow)
                    return false;

                if (signer.Document.Status != "Pending" && signer.Document.Status != "InProgress")
                    return false;

                if (signer.Status == "Signed" || signer.Status == "Declined" || signer.Status == "Expired")
                    return false;

                if (signer.Status == "Invited")
                {
                    signer.Status = "Viewed";
                    signer.ViewedAt = DateTime.UtcNow;
                    signer.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signer access for document {DocumentId} signer {SignerId} for tenant {TenantId}", 
                    documentId, signerId, tenantId);
                throw;
            }
        }

        private async Task<string> GenerateSignatureHashAsync(string signatureData)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signatureData));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private async Task<string> GenerateCertificateFingerprintAsync(string certificateData)
        {
            if (string.IsNullOrEmpty(certificateData))
                return null;

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(certificateData));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private string GetClientIpAddress()
        {
            return "127.0.0.1";
        }

        private async Task CheckDocumentCompletionAsync(int documentId, int tenantId)
        {
            var document = await _context.ESignatureDocuments
                .Include(d => d.Signers)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

            if (document == null)
                return;

            var requiredSigners = document.Signers.Where(s => s.IsRequired && !s.IsDeleted).ToList();
            var signedRequiredSigners = requiredSigners.Where(s => s.Status == "Signed").ToList();
            var declinedRequiredSigners = requiredSigners.Where(s => s.Status == "Declined").ToList();

            if (declinedRequiredSigners.Any())
            {
                document.Status = "Declined";
            }
            else if (document.RequireAllSigners)
            {
                var allSigners = document.Signers.Where(s => !s.IsDeleted).ToList();
                var allSigned = allSigners.All(s => s.Status == "Signed");
                
                if (allSigned)
                {
                    document.Status = "Completed";
                }
                else if (allSigners.Any(s => s.Status == "InProgress"))
                {
                    document.Status = "InProgress";
                }
            }
            else if (signedRequiredSigners.Count == requiredSigners.Count)
            {
                document.Status = "Completed";
            }
            else if (signedRequiredSigners.Any())
            {
                document.Status = "InProgress";
            }

            document.UpdatedAt = DateTime.UtcNow;
        }

        private decimal CalculateAverageCompletionTime(IEnumerable<ESignatureDocument> completedDocuments)
        {
            var completionTimes = completedDocuments
                .Where(d => d.UpdatedAt.HasValue)
                .Select(d => (d.UpdatedAt.Value - d.CreatedAt).TotalHours)
                .ToList();

            return completionTimes.Any() ? (decimal)completionTimes.Average() : 0;
        }

        private async Task SendSigningReminderAsync(ESignatureSigner signer, int tenantId)
        {
            await _notificationService.SendNotificationAsync(new NotificationRequest
            {
                TenantId = tenantId,
                RecipientEmail = signer.Email,
                Subject = "Reminder: Document Signing Request",
                Message = $"This is a reminder that you have a pending document to sign: {signer.Document.Title}",
                NotificationType = "Email"
            });
        }
    }
}
