using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexora.Core.Data;
using Nexora.Core.Interfaces;

namespace Nexora.Infrastructure.Services
{
    public class ReportingService : IReportingService
    {
        private readonly NexoraDbContext _context;
        private readonly ILogger<ReportingService> _logger;
        private readonly IExportService _exportService;
        private readonly ICurrentUserService _currentUserService;

        public ReportingService(
            NexoraDbContext context,
            ILogger<ReportingService> logger,
            IExportService exportService,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _exportService = exportService;
            _currentUserService = currentUserService;
        }

        public async Task<TransactionReport> GenerateTransactionReportAsync(string tenantId, TransactionReportRequest request)
        {
            try
            {
                _logger.LogInformation("Generating transaction report for tenant {TenantId}", tenantId);

                var query = _context.Transactions
                    .Where(t => t.TenantId == tenantId)
                    .Where(t => t.CreatedAt >= request.StartDate && t.CreatedAt <= request.EndDate);

                if (request.PaymentMethods.Any())
                    query = query.Where(t => request.PaymentMethods.Contains(t.PaymentMethod));

                if (request.Currencies.Any())
                    query = query.Where(t => request.Currencies.Contains(t.Currency));

                if (request.Statuses.Any())
                    query = query.Where(t => request.Statuses.Contains(t.Status));

                if (request.MinAmount.HasValue)
                    query = query.Where(t => t.Amount >= request.MinAmount.Value);

                if (request.MaxAmount.HasValue)
                    query = query.Where(t => t.Amount <= request.MaxAmount.Value);

                if (!string.IsNullOrEmpty(request.UserId))
                    query = query.Where(t => t.UserId == request.UserId);

                if (!string.IsNullOrEmpty(request.MerchantId))
                    query = query.Where(t => t.MerchantId == request.MerchantId);

                var totalCount = await query.CountAsync();

                var transactions = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var allTransactions = await _context.Transactions
                    .Where(t => t.TenantId == tenantId)
                    .Where(t => t.CreatedAt >= request.StartDate && t.CreatedAt <= request.EndDate)
                    .ToListAsync();

                var summary = new TransactionReportSummary
                {
                    TotalTransactions = allTransactions.Count,
                    TotalAmount = allTransactions.Sum(t => t.Amount),
                    AverageAmount = allTransactions.Any() ? allTransactions.Average(t => t.Amount) : 0,
                    SuccessfulTransactions = allTransactions.Count(t => t.Status == TransactionStatus.Completed),
                    FailedTransactions = allTransactions.Count(t => t.Status == TransactionStatus.Failed),
                    SuccessRate = allTransactions.Any() ? 
                        (decimal)allTransactions.Count(t => t.Status == TransactionStatus.Completed) / allTransactions.Count * 100 : 0,
                    AmountByCurrency = allTransactions
                        .GroupBy(t => t.Currency)
                        .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount)),
                    TransactionsByPaymentMethod = allTransactions
                        .GroupBy(t => t.PaymentMethod)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    TransactionsByStatus = allTransactions
                        .GroupBy(t => t.Status)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                var items = transactions.Select(t => new TransactionReportItem
                {
                    TransactionId = t.Id,
                    Date = t.CreatedAt,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    PaymentMethod = t.PaymentMethod,
                    Status = t.Status,
                    UserId = t.UserId,
                    MerchantId = t.MerchantId,
                    Description = t.Description,
                    Metadata = new Dictionary<string, object>
                    {
                        { "ProcessingTime", t.ProcessingTime?.TotalMilliseconds ?? 0 },
                        { "Provider", t.Provider ?? "Unknown" }
                    }
                }).ToList();

                var report = new TransactionReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    GeneratedAt = DateTime.UtcNow,
                    Request = request,
                    Summary = summary,
                    Items = items,
                    Charts = GenerateTransactionCharts(allTransactions),
                    Metadata = new ReportMetadata
                    {
                        TotalRecords = totalCount,
                        PageSize = request.PageSize,
                        PageNumber = request.PageNumber,
                        GeneratedBy = _currentUserService.UserId ?? "System"
                    }
                };

                _logger.LogInformation("Transaction report generated successfully for tenant {TenantId}", tenantId);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating transaction report for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<SmsUsageReport> GenerateSmsUsageReportAsync(string tenantId, SmsUsageReportRequest request)
        {
            try
            {
                _logger.LogInformation("Generating SMS usage report for tenant {TenantId}", tenantId);

                var query = _context.SmsMessages
                    .Where(s => s.TenantId == tenantId)
                    .Where(s => s.SentAt >= request.StartDate && s.SentAt <= request.EndDate);

                if (request.Providers.Any())
                    query = query.Where(s => request.Providers.Contains(s.Provider));

                if (request.Countries.Any())
                    query = query.Where(s => request.Countries.Contains(s.Country));

                if (request.MessageTypes.Any())
                    query = query.Where(s => request.MessageTypes.Contains(s.MessageType));

                if (request.DeliveryStatuses.Any())
                    query = query.Where(s => request.DeliveryStatuses.Contains(s.Status));

                if (!string.IsNullOrEmpty(request.SenderId))
                    query = query.Where(s => s.SenderId == request.SenderId);

                if (!string.IsNullOrEmpty(request.CampaignId))
                    query = query.Where(s => s.CampaignId == request.CampaignId);

                var totalCount = await query.CountAsync();

                var messages = await query
                    .OrderByDescending(s => s.SentAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var allMessages = await _context.SmsMessages
                    .Where(s => s.TenantId == tenantId)
                    .Where(s => s.SentAt >= request.StartDate && s.SentAt <= request.EndDate)
                    .ToListAsync();

                var summary = new SmsUsageReportSummary
                {
                    TotalMessages = allMessages.Count,
                    TotalCost = allMessages.Sum(s => s.Cost),
                    AverageCostPerMessage = allMessages.Any() ? allMessages.Average(s => s.Cost) : 0,
                    DeliveredMessages = allMessages.Count(s => s.Status == SmsDeliveryStatus.Delivered),
                    FailedMessages = allMessages.Count(s => s.Status == SmsDeliveryStatus.Failed),
                    DeliveryRate = allMessages.Any() ? 
                        (decimal)allMessages.Count(s => s.Status == SmsDeliveryStatus.Delivered) / allMessages.Count * 100 : 0,
                    MessagesByProvider = allMessages
                        .GroupBy(s => s.Provider)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    MessagesByCountry = allMessages
                        .GroupBy(s => s.Country)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    MessagesByStatus = allMessages
                        .GroupBy(s => s.Status)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                var items = messages.Select(s => new SmsUsageReportItem
                {
                    MessageId = s.Id,
                    SentAt = s.SentAt,
                    PhoneNumber = s.PhoneNumber,
                    Provider = s.Provider,
                    Country = s.Country,
                    MessageType = s.MessageType,
                    Status = s.Status,
                    Cost = s.Cost,
                    Currency = s.Currency,
                    SenderId = s.SenderId,
                    CampaignId = s.CampaignId,
                    Metadata = new Dictionary<string, object>
                    {
                        { "MessageLength", s.MessageLength },
                        { "SegmentCount", s.SegmentCount },
                        { "RetryCount", s.RetryCount }
                    }
                }).ToList();

                var report = new SmsUsageReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    GeneratedAt = DateTime.UtcNow,
                    Request = request,
                    Summary = summary,
                    Items = items,
                    Charts = GenerateSmsUsageCharts(allMessages),
                    CostAnalysis = GenerateCostAnalysis(allMessages),
                    DeliveryAnalysis = GenerateDeliveryAnalysis(allMessages),
                    Metadata = new ReportMetadata
                    {
                        TotalRecords = totalCount,
                        PageSize = request.PageSize,
                        PageNumber = request.PageNumber,
                        GeneratedBy = _currentUserService.UserId ?? "System"
                    }
                };

                _logger.LogInformation("SMS usage report generated successfully for tenant {TenantId}", tenantId);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SMS usage report for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<RevenueReport> GenerateRevenueReportAsync(string tenantId, RevenueReportRequest request)
        {
            try
            {
                _logger.LogInformation("Generating revenue report for tenant {TenantId}", tenantId);

                var transactions = await _context.Transactions
                    .Where(t => t.TenantId == tenantId)
                    .Where(t => t.CreatedAt >= request.StartDate && t.CreatedAt <= request.EndDate)
                    .Where(t => t.Status == TransactionStatus.Completed)
                    .ToListAsync();

                var smsBilling = await _context.SmsMessages
                    .Where(s => s.TenantId == tenantId)
                    .Where(s => s.SentAt >= request.StartDate && s.SentAt <= request.EndDate)
                    .Where(s => s.Status == SmsDeliveryStatus.Delivered)
                    .ToListAsync();

                var transactionRevenue = transactions.Sum(t => t.Amount);
                var smsRevenue = smsBilling.Sum(s => s.Cost);
                var totalRevenue = transactionRevenue + smsRevenue;

                var summary = new RevenueReportSummary
                {
                    TotalRevenue = totalRevenue,
                    GrowthRate = await CalculateGrowthRate(tenantId, request.StartDate, request.EndDate),
                    AverageRevenuePerUser = await CalculateAverageRevenuePerUser(tenantId, request.StartDate, request.EndDate),
                    RevenueByCurrency = transactions
                        .GroupBy(t => t.Currency)
                        .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount)),
                    RevenueByStream = new Dictionary<string, decimal>
                    {
                        { "Payments", transactionRevenue },
                        { "SMS", smsRevenue }
                    }
                };

                var items = GenerateRevenueItems(transactions, smsBilling, request.GroupBy);

                var report = new RevenueReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    GeneratedAt = DateTime.UtcNow,
                    Request = request,
                    Summary = summary,
                    Items = items,
                    Charts = GenerateRevenueCharts(items),
                    Forecast = request.IncludeForecasting ? GenerateRevenueForecast(items, request.ForecastPeriods) : null,
                    Comparison = request.IncludeComparisons ? await GenerateRevenueComparison(tenantId, request) : null,
                    Metadata = new ReportMetadata
                    {
                        TotalRecords = items.Count,
                        GeneratedBy = _currentUserService.UserId ?? "System"
                    }
                };

                _logger.LogInformation("Revenue report generated successfully for tenant {TenantId}", tenantId);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating revenue report for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<ComplianceReport> GenerateComplianceReportAsync(string tenantId, ComplianceReportRequest request)
        {
            try
            {
                _logger.LogInformation("Generating compliance report for tenant {TenantId}", tenantId);

                var auditLogs = await _context.AuditLogs
                    .Where(a => a.TenantId == tenantId)
                    .Where(a => a.Timestamp >= request.StartDate && a.Timestamp <= request.EndDate)
                    .ToListAsync();

                var summary = new ComplianceReportSummary
                {
                    TotalAuditEntries = auditLogs.Count,
                    ComplianceScore = CalculateComplianceScore(auditLogs),
                    ViolationCount = auditLogs.Count(a => a.Action.Contains("VIOLATION")),
                    RiskLevel = AssessRiskLevel(auditLogs)
                };

                var items = auditLogs.Select(a => new ComplianceReportItem
                {
                    Id = a.Id,
                    Timestamp = a.Timestamp,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    UserId = a.UserId,
                    Changes = a.Changes,
                    ComplianceType = DetermineComplianceType(a.Action),
                    RiskScore = CalculateRiskScore(a)
                }).ToList();

                var violations = items
                    .Where(i => i.Action.Contains("VIOLATION"))
                    .Select(i => new ComplianceViolation
                    {
                        Id = i.Id,
                        Type = i.ComplianceType,
                        Severity = DetermineSeverity(i.RiskScore),
                        Description = i.Action,
                        Timestamp = i.Timestamp,
                        UserId = i.UserId,
                        Status = "Open"
                    }).ToList();

                var report = new ComplianceReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    GeneratedAt = DateTime.UtcNow,
                    Request = request,
                    Summary = summary,
                    Items = items,
                    Violations = violations,
                    RiskAssessment = request.IncludeRiskAssessment ? GenerateRiskAssessment(auditLogs) : null,
                    Metadata = new ReportMetadata
                    {
                        TotalRecords = items.Count,
                        GeneratedBy = _currentUserService.UserId ?? "System"
                    }
                };

                _logger.LogInformation("Compliance report generated successfully for tenant {TenantId}", tenantId);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating compliance report for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<CustomReport> GenerateCustomReportAsync(string tenantId, CustomReportRequest request)
        {
            try
            {
                _logger.LogInformation("Generating custom report for tenant {TenantId}", tenantId);

                var data = new List<Dictionary<string, object>>();
                var summary = new Dictionary<string, object>();

                for (int i = 0; i < 100; i++)
                {
                    data.Add(new Dictionary<string, object>
                    {
                        { "Id", i + 1 },
                        { "Date", DateTime.UtcNow.AddDays(-i) },
                        { "Value", Random.Shared.Next(100, 1000) },
                        { "Category", $"Category {(i % 5) + 1}" }
                    });
                }

                summary["TotalRecords"] = data.Count;
                summary["TotalValue"] = data.Sum(d => (int)d["Value"]);
                summary["AverageValue"] = data.Average(d => (int)d["Value"]);

                var report = new CustomReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    GeneratedAt = DateTime.UtcNow,
                    Request = request,
                    Data = data.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList(),
                    Summary = summary,
                    Metadata = new ReportMetadata
                    {
                        TotalRecords = data.Count,
                        PageSize = request.PageSize,
                        PageNumber = request.PageNumber,
                        GeneratedBy = _currentUserService.UserId ?? "System"
                    }
                };

                _logger.LogInformation("Custom report generated successfully for tenant {TenantId}", tenantId);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating custom report for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<byte[]> ExportReportToPdfAsync(string tenantId, string reportId, ExportOptions options)
        {
            throw new NotImplementedException("PDF export will be implemented with the ExportService");
        }

        public async Task<byte[]> ExportReportToExcelAsync(string tenantId, string reportId, ExportOptions options)
        {
            throw new NotImplementedException("Excel export will be implemented with the ExportService");
        }

        public async Task<byte[]> ExportReportToCsvAsync(string tenantId, string reportId, ExportOptions options)
        {
            throw new NotImplementedException("CSV export will be implemented with the ExportService");
        }

        public async Task<List<ReportTemplate>> GetReportTemplatesAsync(string tenantId)
        {
            return new List<ReportTemplate>
            {
                new ReportTemplate
                {
                    Id = "1",
                    Name = "Monthly Transaction Summary",
                    Description = "Monthly overview of all transactions",
                    Type = ReportType.Transaction,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    CreatedBy = "System"
                },
                new ReportTemplate
                {
                    Id = "2",
                    Name = "SMS Usage Analysis",
                    Description = "Detailed SMS usage and cost analysis",
                    Type = ReportType.SmsUsage,
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    CreatedBy = "System"
                }
            };
        }

        public async Task<ReportTemplate> CreateReportTemplateAsync(string tenantId, CreateReportTemplateRequest request)
        {
            return new ReportTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                Configuration = request.Configuration,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId ?? "System"
            };
        }

        public async Task<List<ScheduledReport>> GetScheduledReportsAsync(string tenantId)
        {
            return new List<ScheduledReport>();
        }

        public async Task<ScheduledReport> CreateScheduledReportAsync(string tenantId, CreateScheduledReportRequest request)
        {
            return new ScheduledReport
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                CronExpression = request.CronExpression,
                Configuration = request.Configuration,
                Recipients = request.Recipients,
                IsActive = true
            };
        }

        private List<TransactionReportChart> GenerateTransactionCharts(List<Core.Entities.Transaction> transactions)
        {
            return new List<TransactionReportChart>();
        }

        private List<SmsUsageReportChart> GenerateSmsUsageCharts(List<Core.Entities.SmsMessage> messages)
        {
            return new List<SmsUsageReportChart>();
        }

        private SmsUsageReportCostAnalysis GenerateCostAnalysis(List<Core.Entities.SmsMessage> messages)
        {
            return new SmsUsageReportCostAnalysis();
        }

        private SmsUsageReportDeliveryAnalysis GenerateDeliveryAnalysis(List<Core.Entities.SmsMessage> messages)
        {
            return new SmsUsageReportDeliveryAnalysis();
        }

        private async Task<decimal> CalculateGrowthRate(string tenantId, DateTime startDate, DateTime endDate)
        {
            return 0;
        }

        private async Task<decimal> CalculateAverageRevenuePerUser(string tenantId, DateTime startDate, DateTime endDate)
        {
            return 0;
        }

        private List<RevenueReportItem> GenerateRevenueItems(
            List<Core.Entities.Transaction> transactions, 
            List<Core.Entities.SmsMessage> messages, 
            ReportGroupBy groupBy)
        {
            return new List<RevenueReportItem>();
        }

        private List<RevenueReportChart> GenerateRevenueCharts(List<RevenueReportItem> items)
        {
            return new List<RevenueReportChart>();
        }

        private RevenueForecast GenerateRevenueForecast(List<RevenueReportItem> items, int periods)
        {
            return new RevenueForecast();
        }

        private async Task<RevenueComparison> GenerateRevenueComparison(string tenantId, RevenueReportRequest request)
        {
            return new RevenueComparison();
        }

        private decimal CalculateComplianceScore(List<Core.Entities.AuditLog> auditLogs)
        {
            return 95.5m;
        }

        private string AssessRiskLevel(List<Core.Entities.AuditLog> auditLogs)
        {
            return "Low";
        }

        private ComplianceType DetermineComplianceType(string action)
        {
            return ComplianceType.AML;
        }

        private decimal CalculateRiskScore(ComplianceReportItem item)
        {
            return 1.0m;
        }

        private string DetermineSeverity(decimal riskScore)
        {
            return riskScore > 5 ? "High" : riskScore > 2 ? "Medium" : "Low";
        }

        private ComplianceRiskAssessment GenerateRiskAssessment(List<Core.Entities.AuditLog> auditLogs)
        {
            return new ComplianceRiskAssessment();
        }
    }

    public class ReportMetadata
    {
        public int TotalRecords { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class ComplianceReportSummary
    {
        public int TotalAuditEntries { get; set; }
        public decimal ComplianceScore { get; set; }
        public int ViolationCount { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
    }

    public class ComplianceReportItem
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string Changes { get; set; } = string.Empty;
        public ComplianceType ComplianceType { get; set; }
        public decimal RiskScore { get; set; }
    }

    public class ComplianceViolation
    {
        public string Id { get; set; } = string.Empty;
        public ComplianceType Type { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? UserId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ComplianceRiskAssessment
    {
        public string OverallRisk { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<string, decimal> RiskFactors { get; set; } = new();
    }

    public class CreateReportTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    public class CreateScheduledReportRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public string CronExpression { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<string> Recipients { get; set; } = new();
    }
}
