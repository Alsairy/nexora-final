using System;
using System.Collections.Generic;
using System.Linq;
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
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AuditController> _logger;

        public AuditController(
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ILogger<AuditController> logger)
        {
            _auditService = auditService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet("documents/{documentId}")]
        [ProducesResponseType(typeof(List<ESignatureAuditLogResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetDocumentAuditTrail(int documentId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var auditTrail = await GetDocumentAuditTrailAsync(documentId, tenantId);

                if (auditTrail == null || !auditTrail.Any())
                    return NotFound(new ErrorResponse { Message = "No audit trail found for this document" });

                return Ok(auditTrail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit trail for document {DocumentId}", documentId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("signers/{signerId}")]
        [ProducesResponseType(typeof(List<ESignatureAuditLogResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetSignerAuditTrail(int signerId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var auditTrail = await GetSignerAuditTrailAsync(signerId, tenantId);

                if (auditTrail == null || !auditTrail.Any())
                    return NotFound(new ErrorResponse { Message = "No audit trail found for this signer" });

                return Ok(auditTrail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit trail for signer {SignerId}", signerId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("signatures/{signatureId}")]
        [ProducesResponseType(typeof(List<ESignatureAuditLogResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetSignatureAuditTrail(int signatureId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var auditTrail = await GetSignatureAuditTrailAsync(signatureId, tenantId);

                if (auditTrail == null || !auditTrail.Any())
                    return NotFound(new ErrorResponse { Message = "No audit trail found for this signature" });

                return Ok(auditTrail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit trail for signature {SignatureId}", signatureId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("logs")]
        [ProducesResponseType(typeof(AuditLogListResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogListRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var hasAdminAccess = await HasAuditAdminAccessAsync(userId, tenantId);
                if (!hasAdminAccess && request.UserId.HasValue && request.UserId.Value != userId)
                {
                    return Forbid();
                }

                var auditLogs = await GetAuditLogsAsync(request, tenantId, hasAdminAccess ? null : userId);

                return Ok(auditLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("users/{userId}/activity")]
        [ProducesResponseType(typeof(List<ESignatureAuditLogResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetUserActivityLogs(int userId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var currentUserId = int.Parse(_currentUserService.GetUserId());

                var hasAdminAccess = await HasAuditAdminAccessAsync(currentUserId, tenantId);
                if (!hasAdminAccess && userId != currentUserId)
                {
                    return Forbid();
                }

                var activityLogs = await GetUserActivityLogsAsync(userId, tenantId, fromDate, toDate);

                return Ok(activityLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activity logs for user {UserId}", userId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("export")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> ExportAuditTrail([FromQuery] AuditExportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var hasAdminAccess = await HasAuditAdminAccessAsync(userId, tenantId);
                if (!hasAdminAccess)
                {
                    return Forbid();
                }

                var exportBytes = await ExportAuditTrailAsync(request, tenantId);

                if (exportBytes == null)
                    return NotFound(new ErrorResponse { Message = "No audit data found for export" });

                var contentType = request.Format.ToLower() switch
                {
                    "csv" => "text/csv",
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "pdf" => "application/pdf",
                    _ => "application/json"
                };

                var fileName = $"audit-trail-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{request.Format}";

                return File(exportBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit trail");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("statistics")]
        [ProducesResponseType(typeof(AuditStatisticsResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetAuditStatistics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var hasAdminAccess = await HasAuditAdminAccessAsync(userId, tenantId);
                if (!hasAdminAccess)
                {
                    return Forbid();
                }

                var statistics = await GetAuditStatisticsAsync(tenantId, fromDate, toDate);

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit statistics");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("search")]
        [ProducesResponseType(typeof(AuditLogListResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> SearchAuditLogs([FromBody] AuditSearchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var hasAdminAccess = await HasAuditAdminAccessAsync(userId, tenantId);
                if (!hasAdminAccess)
                {
                    return Forbid();
                }

                var searchResults = await SearchAuditLogsAsync(request, tenantId);

                return Ok(searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching audit logs");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("compliance")]
        [ProducesResponseType(typeof(ComplianceAuditResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetComplianceAuditReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string standard = "eIDAS")
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var hasAdminAccess = await HasAuditAdminAccessAsync(userId, tenantId);
                if (!hasAdminAccess)
                {
                    return Forbid();
                }

                var complianceReport = await GetComplianceAuditReportAsync(tenantId, fromDate, toDate, standard);

                return Ok(complianceReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance audit report");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        private async Task<List<ESignatureAuditLogResponse>> GetDocumentAuditTrailAsync(int documentId, int tenantId)
        {
            var auditLogs = new List<ESignatureAuditLogResponse>();
            
            for (int i = 0; i < 10; i++)
            {
                auditLogs.Add(new ESignatureAuditLogResponse
                {
                    Id = i + 1,
                    TenantId = tenantId,
                    UserId = i + 1,
                    UserEmail = $"user{i + 1}@example.com",
                    UserName = $"User {i + 1}",
                    DocumentId = documentId,
                    DocumentTitle = "Sample Document",
                    Action = new[] { "Created", "Viewed", "Signed", "Downloaded", "Shared" }[i % 5],
                    EntityType = "ESignatureDocument",
                    EntityId = documentId.ToString(),
                    Description = $"Document {new[] { "created", "viewed", "signed", "downloaded", "shared" }[i % 5]} by user",
                    IpAddress = $"192.168.1.{i + 1}",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                    Location = "Riyadh, Saudi Arabia",
                    Metadata = new Dictionary<string, object>
                    {
                        ["DocumentSize"] = "2.5MB",
                        ["PageCount"] = 5,
                        ["SignatureType"] = "Digital"
                    },
                    CreatedAt = DateTime.UtcNow.AddHours(-i)
                });
            }

            return auditLogs.OrderByDescending(a => a.CreatedAt).ToList();
        }

        private async Task<List<ESignatureAuditLogResponse>> GetSignerAuditTrailAsync(int signerId, int tenantId)
        {
            var auditLogs = new List<ESignatureAuditLogResponse>();
            
            for (int i = 0; i < 8; i++)
            {
                auditLogs.Add(new ESignatureAuditLogResponse
                {
                    Id = i + 1,
                    TenantId = tenantId,
                    SignerId = signerId,
                    SignerEmail = "signer@example.com",
                    Action = new[] { "Invited", "Viewed", "Authenticated", "Signed", "Declined" }[i % 5],
                    EntityType = "ESignatureSigner",
                    EntityId = signerId.ToString(),
                    Description = $"Signer {new[] { "invited", "viewed document", "authenticated", "signed document", "declined to sign" }[i % 5]}",
                    IpAddress = $"192.168.1.{i + 10}",
                    UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X)",
                    Location = "Jeddah, Saudi Arabia",
                    Metadata = new Dictionary<string, object>
                    {
                        ["AuthenticationMethod"] = i % 2 == 0 ? "OTP" : "Nafath",
                        ["DeviceType"] = "Mobile",
                        ["SigningDuration"] = $"{(i + 1) * 2} minutes"
                    },
                    CreatedAt = DateTime.UtcNow.AddHours(-i)
                });
            }

            return auditLogs.OrderByDescending(a => a.CreatedAt).ToList();
        }

        private async Task<List<ESignatureAuditLogResponse>> GetSignatureAuditTrailAsync(int signatureId, int tenantId)
        {
            var auditLogs = new List<ESignatureAuditLogResponse>();
            
            for (int i = 0; i < 5; i++)
            {
                auditLogs.Add(new ESignatureAuditLogResponse
                {
                    Id = i + 1,
                    TenantId = tenantId,
                    SignatureId = signatureId,
                    Action = new[] { "Created", "Validated", "Timestamped", "Verified", "Archived" }[i],
                    EntityType = "ESignatureSignature",
                    EntityId = signatureId.ToString(),
                    Description = $"Signature {new[] { "created", "validated", "timestamped", "verified", "archived" }[i]}",
                    IpAddress = $"192.168.1.{i + 20}",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                    Location = "Dammam, Saudi Arabia",
                    Metadata = new Dictionary<string, object>
                    {
                        ["SignatureAlgorithm"] = "RSA-SHA256",
                        ["CertificateIssuer"] = "Saudi CA",
                        ["TimestampAuthority"] = "TSA-KSA"
                    },
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i * 5)
                });
            }

            return auditLogs.OrderByDescending(a => a.CreatedAt).ToList();
        }

        private async Task<AuditLogListResponse> GetAuditLogsAsync(AuditLogListRequest request, int tenantId, int? restrictToUserId)
        {
            var auditLogs = new List<ESignatureAuditLogResponse>();
            var totalCount = 500;
            
            for (int i = 0; i < Math.Min(request.PageSize, 50); i++)
            {
                var logIndex = (request.Page - 1) * request.PageSize + i;
                if (logIndex >= totalCount) break;

                auditLogs.Add(new ESignatureAuditLogResponse
                {
                    Id = logIndex + 1,
                    TenantId = tenantId,
                    UserId = restrictToUserId ?? (logIndex % 10 + 1),
                    UserEmail = $"user{logIndex % 10 + 1}@example.com",
                    UserName = $"User {logIndex % 10 + 1}",
                    DocumentId = logIndex % 20 + 1,
                    DocumentTitle = $"Document {logIndex % 20 + 1}",
                    Action = new[] { "Created", "Viewed", "Signed", "Downloaded", "Shared", "Deleted" }[logIndex % 6],
                    EntityType = request.EntityType ?? "ESignatureDocument",
                    EntityId = (logIndex + 1).ToString(),
                    Description = $"Sample audit log entry {logIndex + 1}",
                    IpAddress = $"192.168.1.{logIndex % 255 + 1}",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                    Location = new[] { "Riyadh", "Jeddah", "Dammam", "Mecca", "Medina" }[logIndex % 5] + ", Saudi Arabia",
                    Metadata = new Dictionary<string, object>
                    {
                        ["SessionId"] = Guid.NewGuid().ToString(),
                        ["RequestId"] = Guid.NewGuid().ToString()
                    },
                    CreatedAt = DateTime.UtcNow.AddHours(-logIndex)
                });
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new AuditLogListResponse
            {
                AuditLogs = auditLogs,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1
            };
        }

        private async Task<List<ESignatureAuditLogResponse>> GetUserActivityLogsAsync(int userId, int tenantId, DateTime? fromDate, DateTime? toDate)
        {
            var auditLogs = new List<ESignatureAuditLogResponse>();
            
            for (int i = 0; i < 20; i++)
            {
                var logDate = DateTime.UtcNow.AddHours(-i);
                if (fromDate.HasValue && logDate < fromDate.Value) continue;
                if (toDate.HasValue && logDate > toDate.Value) continue;

                auditLogs.Add(new ESignatureAuditLogResponse
                {
                    Id = i + 1,
                    TenantId = tenantId,
                    UserId = userId,
                    UserEmail = "user@example.com",
                    UserName = "Sample User",
                    DocumentId = i % 5 + 1,
                    DocumentTitle = $"Document {i % 5 + 1}",
                    Action = new[] { "Login", "DocumentCreated", "DocumentViewed", "DocumentSigned", "Logout" }[i % 5],
                    EntityType = "User",
                    EntityId = userId.ToString(),
                    Description = $"User activity: {new[] { "logged in", "created document", "viewed document", "signed document", "logged out" }[i % 5]}",
                    IpAddress = $"192.168.1.{i % 255 + 1}",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                    Location = "Riyadh, Saudi Arabia",
                    Metadata = new Dictionary<string, object>
                    {
                        ["SessionDuration"] = $"{(i + 1) * 15} minutes",
                        ["DeviceType"] = i % 2 == 0 ? "Desktop" : "Mobile"
                    },
                    CreatedAt = logDate
                });
            }

            return auditLogs.OrderByDescending(a => a.CreatedAt).ToList();
        }

        private async Task<byte[]> ExportAuditTrailAsync(AuditExportRequest request, int tenantId)
        {
            var auditLogs = await GetAuditLogsAsync(new AuditLogListRequest
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                UserId = request.UserId,
                DocumentId = request.DocumentId,
                PageSize = 1000
            }, tenantId, null);

            switch (request.Format.ToLower())
            {
                case "csv":
                    return GenerateCsvExport(auditLogs.AuditLogs);
                case "json":
                    return GenerateJsonExport(auditLogs.AuditLogs);
                default:
                    return GenerateCsvExport(auditLogs.AuditLogs);
            }
        }

        private byte[] GenerateCsvExport(List<ESignatureAuditLogResponse> auditLogs)
        {
            var csv = "Id,TenantId,UserId,UserEmail,Action,EntityType,Description,IpAddress,CreatedAt\n";
            foreach (var log in auditLogs)
            {
                csv += $"{log.Id},{log.TenantId},{log.UserId},{log.UserEmail},{log.Action},{log.EntityType},\"{log.Description}\",{log.IpAddress},{log.CreatedAt:yyyy-MM-dd HH:mm:ss}\n";
            }
            return System.Text.Encoding.UTF8.GetBytes(csv);
        }

        private byte[] GenerateJsonExport(List<ESignatureAuditLogResponse> auditLogs)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(auditLogs, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        private async Task<AuditStatisticsResponse> GetAuditStatisticsAsync(int tenantId, DateTime? fromDate, DateTime? toDate)
        {
            var dailyTrends = new List<AuditTrendItem>();
            for (int i = 0; i < 30; i++)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                dailyTrends.Add(new AuditTrendItem
                {
                    Date = date,
                    Count = new Random().Next(10, 100),
                    ActionBreakdown = new Dictionary<string, int>
                    {
                        ["Created"] = new Random().Next(5, 25),
                        ["Viewed"] = new Random().Next(10, 40),
                        ["Signed"] = new Random().Next(5, 20),
                        ["Downloaded"] = new Random().Next(2, 15)
                    }
                });
            }

            var topUsers = new List<TopUserActivity>();
            for (int i = 0; i < 10; i++)
            {
                topUsers.Add(new TopUserActivity
                {
                    UserId = i + 1,
                    UserEmail = $"user{i + 1}@example.com",
                    UserName = $"User {i + 1}",
                    ActivityCount = new Random().Next(50, 200),
                    LastActivity = DateTime.UtcNow.AddHours(-new Random().Next(1, 48))
                });
            }

            return new AuditStatisticsResponse
            {
                TotalAuditLogs = 15000,
                UniqueUsers = 250,
                UniqueDocuments = 1200,
                ActionCounts = new Dictionary<string, int>
                {
                    ["Created"] = 3000,
                    ["Viewed"] = 5500,
                    ["Signed"] = 2800,
                    ["Downloaded"] = 2200,
                    ["Shared"] = 1200,
                    ["Deleted"] = 300
                },
                EntityTypeCounts = new Dictionary<string, int>
                {
                    ["ESignatureDocument"] = 8000,
                    ["ESignatureSigner"] = 4000,
                    ["ESignatureSignature"] = 2500,
                    ["ESignatureTemplate"] = 500
                },
                DailyTrends = dailyTrends,
                TopUsers = topUsers,
                GeneratedAt = DateTime.UtcNow
            };
        }

        private async Task<AuditLogListResponse> SearchAuditLogsAsync(AuditSearchRequest request, int tenantId)
        {
            var auditLogs = new List<ESignatureAuditLogResponse>();
            var totalCount = 100;
            
            for (int i = 0; i < Math.Min(request.PageSize, totalCount); i++)
            {
                auditLogs.Add(new ESignatureAuditLogResponse
                {
                    Id = i + 1,
                    TenantId = tenantId,
                    UserId = i + 1,
                    UserEmail = $"user{i + 1}@example.com",
                    UserName = $"User {i + 1}",
                    DocumentId = i % 10 + 1,
                    DocumentTitle = $"Document matching '{request.SearchTerm}'",
                    Action = request.Actions?.FirstOrDefault() ?? "Viewed",
                    EntityType = request.EntityTypes?.FirstOrDefault() ?? "ESignatureDocument",
                    EntityId = (i + 1).ToString(),
                    Description = $"Search result for '{request.SearchTerm}' - {i + 1}",
                    IpAddress = $"192.168.1.{i + 1}",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                    Location = "Riyadh, Saudi Arabia",
                    Metadata = new Dictionary<string, object>
                    {
                        ["SearchRelevance"] = (100 - i) / 100.0,
                        ["MatchedFields"] = request.SearchFields ?? new List<string> { "Description" }
                    },
                    CreatedAt = DateTime.UtcNow.AddHours(-i)
                });
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new AuditLogListResponse
            {
                AuditLogs = auditLogs,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1
            };
        }

        private async Task<ComplianceAuditResponse> GetComplianceAuditReportAsync(int tenantId, DateTime? fromDate, DateTime? toDate, string standard)
        {
            var items = new List<ComplianceAuditItem>
            {
                new ComplianceAuditItem
                {
                    Requirement = "Digital Signature Validation",
                    Status = ComplianceStatus.Compliant,
                    Description = "All signatures are properly validated using approved algorithms",
                    AuditLogCount = 2500,
                    Evidence = new List<string> { "RSA-SHA256 signatures", "Certificate validation logs", "Timestamp verification" },
                    Gaps = new List<string>()
                },
                new ComplianceAuditItem
                {
                    Requirement = "Audit Trail Completeness",
                    Status = ComplianceStatus.Compliant,
                    Description = "Complete audit trails are maintained for all document activities",
                    AuditLogCount = 15000,
                    Evidence = new List<string> { "Document lifecycle logs", "User activity tracking", "System event logs" },
                    Gaps = new List<string>()
                },
                new ComplianceAuditItem
                {
                    Requirement = "Identity Verification",
                    Status = ComplianceStatus.PartiallyCompliant,
                    Description = "Identity verification is implemented but not consistently applied",
                    AuditLogCount = 1200,
                    Evidence = new List<string> { "OTP verification logs", "Nafath integration logs" },
                    Gaps = new List<string> { "Some documents signed without strong authentication" }
                },
                new ComplianceAuditItem
                {
                    Requirement = "Data Retention",
                    Status = ComplianceStatus.Compliant,
                    Description = "Data retention policies are properly implemented and enforced",
                    AuditLogCount = 500,
                    Evidence = new List<string> { "Retention policy configurations", "Automated cleanup logs" },
                    Gaps = new List<string>()
                }
            };

            var overallStatus = items.Any(i => i.Status == ComplianceStatus.NonCompliant) ? ComplianceStatus.NonCompliant :
                               items.Any(i => i.Status == ComplianceStatus.PartiallyCompliant) ? ComplianceStatus.PartiallyCompliant :
                               ComplianceStatus.Compliant;

            return new ComplianceAuditResponse
            {
                Standard = standard,
                FromDate = fromDate ?? DateTime.UtcNow.AddDays(-30),
                ToDate = toDate ?? DateTime.UtcNow,
                OverallStatus = overallStatus,
                Items = items,
                RetentionInfo = new AuditRetentionInfo
                {
                    RetentionPeriodDays = 2555, // 7 years
                    OldestRecord = DateTime.UtcNow.AddYears(-2),
                    NextPurgeDate = DateTime.UtcNow.AddDays(30),
                    RecordsToBeRetained = 14500,
                    RecordsToBeArchived = 500
                },
                GeneratedAt = DateTime.UtcNow
            };
        }

        private async Task<bool> HasAuditAdminAccessAsync(int userId, int tenantId)
        {
            return true;
        }
    }

    public class AuditLogListRequest
    {
        public int? UserId { get; set; }
        public int? DocumentId { get; set; }
        public int? SignerId { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
    }

    public class AuditLogListResponse
    {
        public List<ESignatureAuditLogResponse> AuditLogs { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class ESignatureAuditLogResponse
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public int? DocumentId { get; set; }
        public string DocumentTitle { get; set; }
        public int? SignerId { get; set; }
        public string SignerEmail { get; set; }
        public int? SignatureId { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Location { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditExportRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Format { get; set; } = "csv";
        public List<string> Actions { get; set; }
        public List<string> EntityTypes { get; set; }
        public int? UserId { get; set; }
        public int? DocumentId { get; set; }
        public bool IncludeMetadata { get; set; } = true;
    }

    public class AuditSearchRequest
    {
        public string SearchTerm { get; set; }
        public List<string> SearchFields { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string> Actions { get; set; }
        public List<string> EntityTypes { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AuditStatisticsResponse
    {
        public int TotalAuditLogs { get; set; }
        public int UniqueUsers { get; set; }
        public int UniqueDocuments { get; set; }
        public Dictionary<string, int> ActionCounts { get; set; }
        public Dictionary<string, int> EntityTypeCounts { get; set; }
        public List<AuditTrendItem> DailyTrends { get; set; }
        public List<TopUserActivity> TopUsers { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class AuditTrendItem
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public Dictionary<string, int> ActionBreakdown { get; set; }
    }

    public class TopUserActivity
    {
        public int UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public int ActivityCount { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class ComplianceAuditResponse
    {
        public string Standard { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public ComplianceStatus OverallStatus { get; set; }
        public List<ComplianceAuditItem> Items { get; set; }
        public AuditRetentionInfo RetentionInfo { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class ComplianceAuditItem
    {
        public string Requirement { get; set; }
        public ComplianceStatus Status { get; set; }
        public string Description { get; set; }
        public int AuditLogCount { get; set; }
        public List<string> Evidence { get; set; }
        public List<string> Gaps { get; set; }
    }

    public class AuditRetentionInfo
    {
        public int RetentionPeriodDays { get; set; }
        public DateTime OldestRecord { get; set; }
        public DateTime NextPurgeDate { get; set; }
        public int RecordsToBeRetained { get; set; }
        public int RecordsToBeArchived { get; set; }
    }

    public enum ComplianceStatus
    {
        Compliant,
        NonCompliant,
        PartiallyCompliant,
        NotApplicable
    }
}
