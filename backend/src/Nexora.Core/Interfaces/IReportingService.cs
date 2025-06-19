using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexora.Core.Interfaces
{
    public interface IReportingService
    {
        Task<TransactionReport> GenerateTransactionReportAsync(string tenantId, TransactionReportRequest request);
        Task<SmsUsageReport> GenerateSmsUsageReportAsync(string tenantId, SmsUsageReportRequest request);
        Task<RevenueReport> GenerateRevenueReportAsync(string tenantId, RevenueReportRequest request);
        Task<ComplianceReport> GenerateComplianceReportAsync(string tenantId, ComplianceReportRequest request);
        Task<CustomReport> GenerateCustomReportAsync(string tenantId, CustomReportRequest request);
        Task<byte[]> ExportReportToPdfAsync(string tenantId, string reportId, ExportOptions options);
        Task<byte[]> ExportReportToExcelAsync(string tenantId, string reportId, ExportOptions options);
        Task<byte[]> ExportReportToCsvAsync(string tenantId, string reportId, ExportOptions options);
        Task<List<ReportTemplate>> GetReportTemplatesAsync(string tenantId);
        Task<ReportTemplate> CreateReportTemplateAsync(string tenantId, CreateReportTemplateRequest request);
        Task<List<ScheduledReport>> GetScheduledReportsAsync(string tenantId);
        Task<ScheduledReport> CreateScheduledReportAsync(string tenantId, CreateScheduledReportRequest request);
    }

    public class TransactionReportRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> PaymentMethods { get; set; } = new();
        public List<string> Currencies { get; set; } = new();
        public List<TransactionStatus> Statuses { get; set; } = new();
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string? UserId { get; set; }
        public string? MerchantId { get; set; }
        public ReportGroupBy GroupBy { get; set; } = ReportGroupBy.Date;
        public ReportAggregation Aggregation { get; set; } = ReportAggregation.Sum;
        public int PageSize { get; set; } = 1000;
        public int PageNumber { get; set; } = 1;
    }

    public class SmsUsageReportRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> Providers { get; set; } = new();
        public List<string> Countries { get; set; } = new();
        public List<SmsMessageType> MessageTypes { get; set; } = new();
        public List<SmsDeliveryStatus> DeliveryStatuses { get; set; } = new();
        public string? SenderId { get; set; }
        public string? CampaignId { get; set; }
        public ReportGroupBy GroupBy { get; set; } = ReportGroupBy.Date;
        public bool IncludeCostAnalysis { get; set; } = true;
        public bool IncludeDeliveryAnalysis { get; set; } = true;
        public int PageSize { get; set; } = 1000;
        public int PageNumber { get; set; } = 1;
    }

    public class RevenueReportRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> RevenueStreams { get; set; } = new();
        public List<string> Currencies { get; set; } = new();
        public ReportGroupBy GroupBy { get; set; } = ReportGroupBy.Month;
        public bool IncludeForecasting { get; set; } = false;
        public int ForecastPeriods { get; set; } = 12;
        public bool IncludeComparisons { get; set; } = true;
        public RevenueMetricType MetricType { get; set; } = RevenueMetricType.Gross;
    }

    public class ComplianceReportRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ComplianceType> ComplianceTypes { get; set; } = new();
        public List<string> Jurisdictions { get; set; } = new();
        public bool IncludeAuditTrail { get; set; } = true;
        public bool IncludeRiskAssessment { get; set; } = true;
    }

    public class CustomReportRequest
    {
        public string ReportName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ReportDataSource> DataSources { get; set; } = new();
        public List<ReportFilter> Filters { get; set; } = new();
        public List<ReportColumn> Columns { get; set; } = new();
        public List<ReportAggregateColumn> AggregateColumns { get; set; } = new();
        public ReportGroupBy GroupBy { get; set; } = ReportGroupBy.None;
        public List<ReportSort> Sorting { get; set; } = new();
        public int PageSize { get; set; } = 1000;
        public int PageNumber { get; set; } = 1;
    }

    public class TransactionReport
    {
        public string ReportId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public TransactionReportRequest Request { get; set; } = new();
        public TransactionReportSummary Summary { get; set; } = new();
        public List<TransactionReportItem> Items { get; set; } = new();
        public List<TransactionReportChart> Charts { get; set; } = new();
        public ReportMetadata Metadata { get; set; } = new();
    }

    public class SmsUsageReport
    {
        public string ReportId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public SmsUsageReportRequest Request { get; set; } = new();
        public SmsUsageReportSummary Summary { get; set; } = new();
        public List<SmsUsageReportItem> Items { get; set; } = new();
        public List<SmsUsageReportChart> Charts { get; set; } = new();
        public SmsUsageReportCostAnalysis CostAnalysis { get; set; } = new();
        public SmsUsageReportDeliveryAnalysis DeliveryAnalysis { get; set; } = new();
        public ReportMetadata Metadata { get; set; } = new();
    }

    public class RevenueReport
    {
        public string ReportId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public RevenueReportRequest Request { get; set; } = new();
        public RevenueReportSummary Summary { get; set; } = new();
        public List<RevenueReportItem> Items { get; set; } = new();
        public List<RevenueReportChart> Charts { get; set; } = new();
        public RevenueForecast? Forecast { get; set; }
        public RevenueComparison? Comparison { get; set; }
        public ReportMetadata Metadata { get; set; } = new();
    }

    public class ComplianceReport
    {
        public string ReportId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public ComplianceReportRequest Request { get; set; } = new();
        public ComplianceReportSummary Summary { get; set; } = new();
        public List<ComplianceReportItem> Items { get; set; } = new();
        public List<ComplianceViolation> Violations { get; set; } = new();
        public ComplianceRiskAssessment? RiskAssessment { get; set; }
        public ReportMetadata Metadata { get; set; } = new();
    }

    public class CustomReport
    {
        public string ReportId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public CustomReportRequest Request { get; set; } = new();
        public List<Dictionary<string, object>> Data { get; set; } = new();
        public Dictionary<string, object> Summary { get; set; } = new();
        public ReportMetadata Metadata { get; set; } = new();
    }

    public class TransactionReportSummary
    {
        public int TotalTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal SuccessRate { get; set; }
        public Dictionary<string, decimal> AmountByCurrency { get; set; } = new();
        public Dictionary<string, int> TransactionsByPaymentMethod { get; set; } = new();
        public Dictionary<TransactionStatus, int> TransactionsByStatus { get; set; } = new();
    }

    public class TransactionReportItem
    {
        public string TransactionId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; }
        public string? UserId { get; set; }
        public string? MerchantId { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SmsUsageReportSummary
    {
        public int TotalMessages { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageCostPerMessage { get; set; }
        public int DeliveredMessages { get; set; }
        public int FailedMessages { get; set; }
        public decimal DeliveryRate { get; set; }
        public Dictionary<string, int> MessagesByProvider { get; set; } = new();
        public Dictionary<string, int> MessagesByCountry { get; set; } = new();
        public Dictionary<SmsDeliveryStatus, int> MessagesByStatus { get; set; } = new();
    }

    public class SmsUsageReportItem
    {
        public string MessageId { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public SmsMessageType MessageType { get; set; }
        public SmsDeliveryStatus Status { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? SenderId { get; set; }
        public string? CampaignId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class RevenueReportSummary
    {
        public decimal TotalRevenue { get; set; }
        public decimal GrowthRate { get; set; }
        public decimal AverageRevenuePerUser { get; set; }
        public Dictionary<string, decimal> RevenueByCurrency { get; set; } = new();
        public Dictionary<string, decimal> RevenueByStream { get; set; } = new();
        public Dictionary<string, decimal> RevenueByPeriod { get; set; } = new();
    }

    public class RevenueReportItem
    {
        public DateTime Period { get; set; }
        public string RevenueStream { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal GrowthRate { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ExportOptions
    {
        public string FileName { get; set; } = string.Empty;
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeSummary { get; set; } = true;
        public bool IncludeMetadata { get; set; } = false;
        public string? WatermarkText { get; set; }
        public Dictionary<string, object> CustomOptions { get; set; } = new();
    }

    public class ReportTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class ScheduledReport
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public string CronExpression { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<string> Recipients { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime? LastRun { get; set; }
        public DateTime? NextRun { get; set; }
    }

    public enum ReportGroupBy
    {
        None,
        Date,
        Week,
        Month,
        Quarter,
        Year,
        PaymentMethod,
        Currency,
        Status,
        Provider,
        Country
    }

    public enum ReportAggregation
    {
        Sum,
        Average,
        Count,
        Min,
        Max
    }

    public enum RevenueMetricType
    {
        Gross,
        Net,
        Recurring,
        OneTime
    }

    public enum ComplianceType
    {
        AML,
        KYC,
        PCI,
        GDPR,
        PSD2,
        SAMA
    }

    public enum ReportType
    {
        Transaction,
        SmsUsage,
        Revenue,
        Compliance,
        Custom
    }

    public enum SmsMessageType
    {
        Transactional,
        Marketing,
        OTP,
        Alert,
        Notification
    }

    public enum TransactionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Refunded
    }

    public class TransactionReportChart { }
    public class SmsUsageReportChart { }
    public class SmsUsageReportCostAnalysis { }
    public class SmsUsageReportDeliveryAnalysis { }
    public class RevenueReportChart { }
    public class RevenueForecast { }
    public class RevenueComparison { }
    public class ComplianceReportSummary { }
    public class ComplianceReportItem { }
    public class ComplianceViolation { }
    public class ComplianceRiskAssessment { }
    public class ReportMetadata { }
    public class ReportDataSource { }
    public class ReportFilter { }
    public class ReportColumn { }
    public class ReportAggregateColumn { }
    public class ReportSort { }
    public class CreateReportTemplateRequest { }
    public class CreateScheduledReportRequest { }
}
