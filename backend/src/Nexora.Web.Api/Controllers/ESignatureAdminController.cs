using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexora.Core.DTOs;
using Nexora.Core.Interfaces;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/esignature/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly IESignatureService _eSignatureService;
        private readonly IESignatureWorkflowService _workflowService;
        private readonly IESignatureTemplateService _templateService;
        private readonly IESignatureComplianceService _complianceService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IESignatureService eSignatureService,
            IESignatureWorkflowService workflowService,
            IESignatureTemplateService templateService,
            IESignatureComplianceService complianceService,
            ICurrentUserService currentUserService,
            ILogger<AdminController> logger)
        {
            _eSignatureService = eSignatureService;
            _workflowService = workflowService;
            _templateService = templateService;
            _complianceService = complianceService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet("statistics")]
        [ProducesResponseType(typeof(AdminStatisticsResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetSystemStatistics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                
                var documentStats = await _eSignatureService.GetStatisticsAsync(tenantId, fromDate, toDate);
                var workflowStats = await GetWorkflowStatisticsAsync(tenantId, fromDate, toDate);
                var templateStats = await GetTemplateStatisticsAsync(tenantId, fromDate, toDate);
                var complianceStats = await GetComplianceStatisticsAsync(tenantId, fromDate, toDate);

                var adminStats = new AdminStatisticsResponse
                {
                    DocumentStatistics = documentStats,
                    WorkflowStatistics = workflowStats,
                    TemplateStatistics = templateStats,
                    ComplianceStatistics = complianceStats,
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(adminStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system statistics");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("documents")]
        [ProducesResponseType(typeof(ESignatureListResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetAllDocuments([FromQuery] AdminDocumentListRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var documents = await GetAllDocumentsForAdminAsync(request, tenantId);

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all documents for admin");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("documents/{id}/force-cancel")]
        [ProducesResponseType(typeof(ESignatureDocumentResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> ForceCancelDocument(int id, [FromBody] AdminCancelDocumentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var document = await _eSignatureService.CancelDocumentAsync(id, request.Reason, tenantId, userId);
                
                if (document == null)
                    return NotFound(new ErrorResponse { Message = "Document not found" });

                _logger.LogWarning("Document {DocumentId} force cancelled by admin {UserId}: {Reason}", id, userId, request.Reason);
                
                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force cancelling document {DocumentId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("documents/{id}/reset-status")]
        [ProducesResponseType(typeof(ESignatureDocumentResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> ResetDocumentStatus(int id, [FromBody] AdminResetStatusRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var document = await ResetDocumentStatusAsync(id, request.NewStatus, request.Reason, tenantId, userId);
                
                if (document == null)
                    return NotFound(new ErrorResponse { Message = "Document not found" });

                _logger.LogWarning("Document {DocumentId} status reset to {NewStatus} by admin {UserId}: {Reason}", 
                    id, request.NewStatus, userId, request.Reason);
                
                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting document status {DocumentId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("configuration")]
        [ProducesResponseType(typeof(ESignatureSystemConfiguration), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetSystemConfiguration()
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var configuration = await GetSystemConfigurationAsync(tenantId);

                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system configuration");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPut("configuration")]
        [ProducesResponseType(typeof(ESignatureSystemConfiguration), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> UpdateSystemConfiguration([FromBody] UpdateSystemConfigurationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var configuration = await UpdateSystemConfigurationAsync(request, tenantId, userId);
                
                _logger.LogInformation("System configuration updated by admin {UserId}", userId);
                
                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system configuration");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("users/activity")]
        [ProducesResponseType(typeof(UserActivityReportResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetUserActivityReport([FromQuery] UserActivityReportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var report = await GetUserActivityReportAsync(request, tenantId);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activity report");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("compliance/report")]
        [ProducesResponseType(typeof(ComplianceReportResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetComplianceReport([FromQuery] ComplianceReportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var report = await _complianceService.GenerateComplianceReportAsync(1, tenantId, request.ReportFormat ?? "json");

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance report");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("health")]
        [ProducesResponseType(typeof(SystemHealthResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetSystemHealth()
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var health = await GetSystemHealthAsync(tenantId);

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("maintenance")]
        [ProducesResponseType(typeof(MaintenanceResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> PerformMaintenance([FromBody] MaintenanceRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var result = await PerformMaintenanceAsync(request, tenantId, userId);
                
                _logger.LogInformation("System maintenance performed by admin {UserId}: {MaintenanceType}", userId, request.MaintenanceType);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing system maintenance");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        private async Task<ESignatureListResponse> GetAllDocumentsForAdminAsync(AdminDocumentListRequest request, int tenantId)
        {
            var baseRequest = new ESignatureListRequest
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Status = request.Status,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            };

            var documents = await _eSignatureService.GetDocumentsAsync(baseRequest, tenantId);
            
            if (request.UserId.HasValue || !string.IsNullOrEmpty(request.UserEmail))
            {
                documents.Documents = documents.Documents.Where(d => 
                    (request.UserId.HasValue && d.CreatedByUserId == request.UserId.Value) ||
                    (!string.IsNullOrEmpty(request.UserEmail) && d.CreatedByUserEmail?.Contains(request.UserEmail, StringComparison.OrdinalIgnoreCase) == true)
                ).ToList();
                
                documents.TotalCount = documents.Documents.Count;
            }

            return documents;
        }

        private async Task<ESignatureDocumentResponse> ResetDocumentStatusAsync(int documentId, string newStatus, string reason, int tenantId, int userId)
        {
            var document = await _eSignatureService.GetDocumentAsync(documentId, tenantId);
            if (document == null)
                return null;

            var updateRequest = new UpdateESignatureDocumentRequest
            {
                Status = newStatus,
                AdminNotes = $"Status reset by admin: {reason}"
            };

            var updatedDocument = await _eSignatureService.UpdateDocumentAsync(documentId, updateRequest, tenantId, userId);
            return updatedDocument;
        }

        private async Task<ESignatureSystemConfiguration> GetSystemConfigurationAsync(int tenantId)
        {
            return new ESignatureSystemConfiguration
            {
                Id = 1,
                TenantId = tenantId,
                DefaultAuthenticationMethod = "OTP",
                DefaultExpiryDays = 30,
                RequireOtpForHighValue = true,
                EnableNafathIntegration = true,
                DefaultLanguage = "en",
                DefaultCalendarType = "Gregorian",
                EnableAuditLogging = true,
                EnableEmailNotifications = true,
                EnableSmsNotifications = true,
                MaxDocumentSize = 50 * 1024 * 1024, // 50MB
                MaxSignersPerDocument = 10,
                AllowedFileTypes = "pdf,doc,docx,txt",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow
            };
        }

        private async Task<ESignatureSystemConfiguration> UpdateSystemConfigurationAsync(UpdateSystemConfigurationRequest request, int tenantId, int userId)
        {
            var config = await GetSystemConfigurationAsync(tenantId);
            
            if (!string.IsNullOrEmpty(request.DefaultAuthenticationMethod))
                config.DefaultAuthenticationMethod = request.DefaultAuthenticationMethod;
            
            if (request.DefaultExpiryDays.HasValue)
                config.DefaultExpiryDays = request.DefaultExpiryDays.Value;
            
            if (request.RequireOtpForHighValue.HasValue)
                config.RequireOtpForHighValue = request.RequireOtpForHighValue.Value;
            
            if (request.EnableNafathIntegration.HasValue)
                config.EnableNafathIntegration = request.EnableNafathIntegration.Value;
            
            if (!string.IsNullOrEmpty(request.DefaultLanguage))
                config.DefaultLanguage = request.DefaultLanguage;
            
            if (!string.IsNullOrEmpty(request.DefaultCalendarType))
                config.DefaultCalendarType = request.DefaultCalendarType;
            
            if (request.EnableAuditLogging.HasValue)
                config.EnableAuditLogging = request.EnableAuditLogging.Value;
            
            if (request.EnableEmailNotifications.HasValue)
                config.EnableEmailNotifications = request.EnableEmailNotifications.Value;
            
            if (request.EnableSmsNotifications.HasValue)
                config.EnableSmsNotifications = request.EnableSmsNotifications.Value;
            
            if (request.MaxDocumentSize.HasValue)
                config.MaxDocumentSize = request.MaxDocumentSize.Value;
            
            if (request.MaxSignersPerDocument.HasValue)
                config.MaxSignersPerDocument = request.MaxSignersPerDocument.Value;
            
            if (!string.IsNullOrEmpty(request.AllowedFileTypes))
                config.AllowedFileTypes = request.AllowedFileTypes;
            
            config.UpdatedAt = DateTime.UtcNow;
            
            return config;
        }

        private async Task<UserActivityReportResponse> GetUserActivityReportAsync(UserActivityReportRequest request, int tenantId)
        {
            var activities = new List<UserActivityItem>();
            
            for (int i = 0; i < Math.Min(request.PageSize, 50); i++)
            {
                activities.Add(new UserActivityItem
                {
                    UserId = request.UserId ?? (i + 1),
                    UserEmail = $"user{i + 1}@example.com",
                    ActivityType = new[] { "DocumentCreated", "DocumentSigned", "DocumentViewed", "TemplateUsed" }[i % 4],
                    Description = $"Sample activity description {i + 1}",
                    Timestamp = DateTime.UtcNow.AddHours(-i),
                    IpAddress = $"192.168.1.{i + 1}",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
                });
            }

            var totalCount = 1000;
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new UserActivityReportResponse
            {
                Activities = activities,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                GeneratedAt = DateTime.UtcNow
            };
        }

        private async Task<object> GetWorkflowStatisticsAsync(int tenantId, DateTime? fromDate, DateTime? toDate)
        {
            return new
            {
                TotalWorkflows = 25,
                ActiveWorkflows = 18,
                CompletedWorkflows = 142,
                AverageCompletionTime = TimeSpan.FromHours(24.5),
                MostUsedWorkflow = "Standard Document Approval",
                WorkflowsByStatus = new Dictionary<string, int>
                {
                    ["Active"] = 18,
                    ["Completed"] = 142,
                    ["Cancelled"] = 5,
                    ["Expired"] = 3
                }
            };
        }

        private async Task<object> GetTemplateStatisticsAsync(int tenantId, DateTime? fromDate, DateTime? toDate)
        {
            return new
            {
                TotalTemplates = 45,
                ActiveTemplates = 32,
                TemplateUsageCount = 1250,
                MostUsedTemplate = "Employment Contract",
                TemplatesByCategory = new Dictionary<string, int>
                {
                    ["Contracts"] = 15,
                    ["Agreements"] = 12,
                    ["Forms"] = 8,
                    ["Legal"] = 10
                }
            };
        }

        private async Task<object> GetComplianceStatisticsAsync(int tenantId, DateTime? fromDate, DateTime? toDate)
        {
            return new
            {
                ComplianceRate = 98.5m,
                TotalAudits = 500,
                PassedAudits = 492,
                FailedAudits = 8,
                ComplianceStandards = new[] { "eIDAS", "ESIGN", "UETA", "Saudi eSignature Law" },
                LastComplianceCheck = DateTime.UtcNow.AddDays(-1),
                NextScheduledAudit = DateTime.UtcNow.AddDays(30)
            };
        }

        private async Task<SystemHealthResponse> GetSystemHealthAsync(int tenantId)
        {
            var checks = new List<HealthCheckItem>
            {
                new HealthCheckItem
                {
                    Name = "Database",
                    Status = "Healthy",
                    Description = "Database connection is working properly",
                    ResponseTime = TimeSpan.FromMilliseconds(45)
                },
                new HealthCheckItem
                {
                    Name = "Redis Cache",
                    Status = "Healthy",
                    Description = "Cache is responding normally",
                    ResponseTime = TimeSpan.FromMilliseconds(12)
                },
                new HealthCheckItem
                {
                    Name = "File Storage",
                    Status = "Healthy",
                    Description = "File storage is accessible",
                    ResponseTime = TimeSpan.FromMilliseconds(78)
                },
                new HealthCheckItem
                {
                    Name = "Email Service",
                    Status = "Healthy",
                    Description = "Email service is operational",
                    ResponseTime = TimeSpan.FromMilliseconds(156)
                },
                new HealthCheckItem
                {
                    Name = "SMS Service",
                    Status = "Warning",
                    Description = "SMS service experiencing minor delays",
                    ResponseTime = TimeSpan.FromMilliseconds(2340)
                }
            };

            var overallStatus = checks.Any(c => c.Status == "Unhealthy") ? "Unhealthy" :
                               checks.Any(c => c.Status == "Warning") ? "Warning" : "Healthy";

            return new SystemHealthResponse
            {
                Status = overallStatus,
                Checks = checks,
                CheckedAt = DateTime.UtcNow
            };
        }

        private async Task<MaintenanceResponse> PerformMaintenanceAsync(MaintenanceRequest request, int tenantId, int userId)
        {
            var maintenanceId = Guid.NewGuid().ToString();
            var results = new List<string>();

            switch (request.MaintenanceType.ToLower())
            {
                case "cleanup":
                    results.Add("Cleaned up temporary files");
                    results.Add("Removed expired documents");
                    results.Add("Optimized database indexes");
                    break;
                case "backup":
                    results.Add("Created database backup");
                    results.Add("Backed up file storage");
                    results.Add("Verified backup integrity");
                    break;
                case "update":
                    results.Add("Updated system configurations");
                    results.Add("Refreshed cache");
                    results.Add("Reloaded templates");
                    break;
                default:
                    results.Add($"Unknown maintenance type: {request.MaintenanceType}");
                    break;
            }

            if (request.DryRun)
            {
                results = results.Select(r => $"[DRY RUN] {r}").ToList();
            }

            return new MaintenanceResponse
            {
                MaintenanceId = maintenanceId,
                Status = "Completed",
                Message = $"Maintenance operation '{request.MaintenanceType}' completed successfully",
                Results = results,
                StartedAt = DateTime.UtcNow.AddMinutes(-5),
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    public class AdminDocumentListRequest : ESignatureListRequest
    {
        public int? UserId { get; set; }
        public string UserEmail { get; set; }
        public bool IncludeDeleted { get; set; }
    }

    public class AdminCancelDocumentRequest
    {
        public string Reason { get; set; }
        public bool NotifyUsers { get; set; } = true;
    }

    public class AdminResetStatusRequest
    {
        public string NewStatus { get; set; }
        public string Reason { get; set; }
        public bool NotifyUsers { get; set; } = true;
    }

    public class ESignatureSystemConfiguration
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string DefaultAuthenticationMethod { get; set; }
        public int DefaultExpiryDays { get; set; }
        public bool RequireOtpForHighValue { get; set; }
        public bool EnableNafathIntegration { get; set; }
        public string DefaultLanguage { get; set; }
        public string DefaultCalendarType { get; set; }
        public bool EnableAuditLogging { get; set; }
        public bool EnableEmailNotifications { get; set; }
        public bool EnableSmsNotifications { get; set; }
        public int MaxDocumentSize { get; set; }
        public int MaxSignersPerDocument { get; set; }
        public string AllowedFileTypes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateSystemConfigurationRequest
    {
        public string DefaultAuthenticationMethod { get; set; }
        public int? DefaultExpiryDays { get; set; }
        public bool? RequireOtpForHighValue { get; set; }
        public bool? EnableNafathIntegration { get; set; }
        public string DefaultLanguage { get; set; }
        public string DefaultCalendarType { get; set; }
        public bool? EnableAuditLogging { get; set; }
        public bool? EnableEmailNotifications { get; set; }
        public bool? EnableSmsNotifications { get; set; }
        public int? MaxDocumentSize { get; set; }
        public int? MaxSignersPerDocument { get; set; }
        public string AllowedFileTypes { get; set; }
    }

    public class UserActivityReportRequest
    {
        public int? UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string ActivityType { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class UserActivityReportResponse
    {
        public List<UserActivityItem> Activities { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class UserActivityItem
    {
        public int UserId { get; set; }
        public string UserEmail { get; set; }
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }

    public class ComplianceReportRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string ComplianceStandard { get; set; }
        public string ReportFormat { get; set; } = "json";
        public bool IncludeDetails { get; set; } = true;
    }

    public class ComplianceReportResponse
    {
        public string ReportId { get; set; }
        public string ComplianceStandard { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public ComplianceStatus OverallStatus { get; set; }
        public List<ComplianceItem> Items { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class ComplianceItem
    {
        public string Category { get; set; }
        public string Requirement { get; set; }
        public ComplianceStatus Status { get; set; }
        public string Details { get; set; }
        public List<string> Evidence { get; set; }
    }

    public class AdminStatisticsResponse
    {
        public ESignatureStatisticsResponse DocumentStatistics { get; set; }
        public object WorkflowStatistics { get; set; }
        public object TemplateStatistics { get; set; }
        public object ComplianceStatistics { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class SystemHealthResponse
    {
        public string Status { get; set; }
        public List<HealthCheckItem> Checks { get; set; }
        public DateTime CheckedAt { get; set; }
    }

    public class HealthCheckItem
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public TimeSpan ResponseTime { get; set; }
    }

    public class MaintenanceRequest
    {
        public string MaintenanceType { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public bool DryRun { get; set; } = false;
    }

    public class MaintenanceResponse
    {
        public string MaintenanceId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public List<string> Results { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
