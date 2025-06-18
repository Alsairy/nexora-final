using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.DTOs
{
    public class SmsAnalyticsDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Period { get; set; }
        public string CampaignId { get; set; }
        public string SenderId { get; set; }
        public string MessageType { get; set; }
        public string ProviderId { get; set; }
        public string Country { get; set; }
        public string Network { get; set; }
        public int MessagesSent { get; set; }
        public int MessagesDelivered { get; set; }
        public int MessagesFailed { get; set; }
        public int MessagesPending { get; set; }
        public int MessagesQueued { get; set; }
        public int MessagesRejected { get; set; }
        public int MessagesExpired { get; set; }
        public int MessagesUnknown { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal FailureRate { get; set; }
        public decimal AverageDeliveryTime { get; set; }
        public decimal MinDeliveryTime { get; set; }
        public decimal MaxDeliveryTime { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageCostPerMessage { get; set; }
        public string Currency { get; set; }
        public int UniqueRecipients { get; set; }
        public int RetryAttempts { get; set; }
        public int ComplianceViolations { get; set; }
        public int DndBlocks { get; set; }
        public int TimeWindowBlocks { get; set; }
        public int ContentFilterBlocks { get; set; }
        public int RateLimitBlocks { get; set; }
        public int OptOutRequests { get; set; }
        public int SpamReports { get; set; }
        public decimal OptOutRate { get; set; }
        public decimal SpamRate { get; set; }
        public decimal EngagementRate { get; set; }
        public int ClickThroughs { get; set; }
        public int Conversions { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal Revenue { get; set; }
        public decimal Roi { get; set; }
        public int ApiCalls { get; set; }
        public int ApiErrors { get; set; }
        public decimal AverageApiResponseTime { get; set; }
        public string TopErrorCodes { get; set; }
        public string TopFailureReasons { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SmsAnalyticsRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Period { get; set; } = "Daily"; // Daily, Weekly, Monthly
        public string CampaignId { get; set; }
        public string SenderId { get; set; }
        public string MessageType { get; set; }
        public string ProviderId { get; set; }
        public string Country { get; set; } = "SA";
        public string Network { get; set; }
        public string GroupBy { get; set; } // Date, Campaign, SenderId, MessageType, Provider, Network
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class SmsAnalyticsResponse
    {
        public List<SmsAnalyticsDto> Analytics { get; set; } = new List<SmsAnalyticsDto>();
        public SmsAnalyticsSummaryDto Summary { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class SmsAnalyticsSummaryDto
    {
        public int TotalMessagesSent { get; set; }
        public int TotalMessagesDelivered { get; set; }
        public int TotalMessagesFailed { get; set; }
        public int TotalMessagesPending { get; set; }
        public decimal OverallDeliveryRate { get; set; }
        public decimal OverallFailureRate { get; set; }
        public decimal AverageDeliveryTime { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageCostPerMessage { get; set; }
        public string Currency { get; set; } = "SAR";
        public int TotalUniqueRecipients { get; set; }
        public int TotalRetryAttempts { get; set; }
        public int TotalComplianceViolations { get; set; }
        public int TotalOptOutRequests { get; set; }
        public int TotalSpamReports { get; set; }
        public decimal OverallOptOutRate { get; set; }
        public decimal OverallSpamRate { get; set; }
        public decimal OverallEngagementRate { get; set; }
        public int TotalClickThroughs { get; set; }
        public int TotalConversions { get; set; }
        public decimal OverallConversionRate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal OverallRoi { get; set; }
        public int TotalApiCalls { get; set; }
        public int TotalApiErrors { get; set; }
        public decimal AverageApiResponseTime { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class SmsDashboardDto
    {
        public SmsAnalyticsSummaryDto TodayStats { get; set; }
        public SmsAnalyticsSummaryDto WeekStats { get; set; }
        public SmsAnalyticsSummaryDto MonthStats { get; set; }
        public List<SmsAnalyticsChartDataDto> DeliveryRateChart { get; set; } = new List<SmsAnalyticsChartDataDto>();
        public List<SmsAnalyticsChartDataDto> VolumeChart { get; set; } = new List<SmsAnalyticsChartDataDto>();
        public List<SmsAnalyticsChartDataDto> CostChart { get; set; } = new List<SmsAnalyticsChartDataDto>();
        public List<SmsProviderPerformanceDto> ProviderPerformance { get; set; } = new List<SmsProviderPerformanceDto>();
        public List<SmsCampaignPerformanceDto> TopCampaigns { get; set; } = new List<SmsCampaignPerformanceDto>();
        public List<SmsErrorAnalysisDto> TopErrors { get; set; } = new List<SmsErrorAnalysisDto>();
        public SmsComplianceStatsDto ComplianceStats { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class SmsAnalyticsChartDataDto
    {
        public DateTime Date { get; set; }
        public string Label { get; set; }
        public decimal Value { get; set; }
        public string Category { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class SmsProviderPerformanceDto
    {
        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public int MessagesSent { get; set; }
        public int MessagesDelivered { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal AverageDeliveryTime { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageCostPerMessage { get; set; }
        public string Currency { get; set; }
        public string HealthStatus { get; set; }
        public int FailureCount { get; set; }
        public string TopErrorCode { get; set; }
        public DateTime LastUsed { get; set; }
    }

    public class SmsCampaignPerformanceDto
    {
        public string CampaignId { get; set; }
        public string CampaignName { get; set; }
        public int MessagesSent { get; set; }
        public int MessagesDelivered { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal TotalCost { get; set; }
        public string Currency { get; set; }
        public int ClickThroughs { get; set; }
        public int Conversions { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal Revenue { get; set; }
        public decimal Roi { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
    }

    public class SmsErrorAnalysisDto
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; }
        public string Resolution { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
    }

    public class SmsComplianceStatsDto
    {
        public int TotalComplianceChecks { get; set; }
        public int ComplianceViolations { get; set; }
        public int DndBlocks { get; set; }
        public int TimeWindowBlocks { get; set; }
        public int ContentFilterBlocks { get; set; }
        public int RateLimitBlocks { get; set; }
        public decimal ComplianceRate { get; set; }
        public List<SmsComplianceViolationDto> TopViolations { get; set; } = new List<SmsComplianceViolationDto>();
        public Dictionary<string, int> ViolationsByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ViolationsByHour { get; set; } = new Dictionary<string, int>();
        public DateTime LastUpdated { get; set; }
    }

    public class SmsComplianceViolationDto
    {
        public string ViolationType { get; set; }
        public string Description { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public string Severity { get; set; }
        public string Resolution { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
    }

    public class SmsReportRequest
    {
        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } // Summary, Detailed, Compliance, Financial

        [MaxLength(50)]
        public string Format { get; set; } = "JSON"; // JSON, CSV, PDF, Excel

        public string CampaignId { get; set; }
        public string SenderId { get; set; }
        public string MessageType { get; set; }
        public string ProviderId { get; set; }
        public string GroupBy { get; set; }
        public bool IncludeCharts { get; set; } = false;
        public bool IncludeRawData { get; set; } = false;
        public string TimeZone { get; set; } = "Asia/Riyadh";
    }

    public class SmsReportResponse
    {
        public string ReportId { get; set; }
        public string ReportType { get; set; }
        public string Format { get; set; }
        public string Status { get; set; }
        public string DownloadUrl { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public long FileSizeBytes { get; set; }
        public SmsAnalyticsSummaryDto Summary { get; set; }
        public object ReportData { get; set; }
    }
}
