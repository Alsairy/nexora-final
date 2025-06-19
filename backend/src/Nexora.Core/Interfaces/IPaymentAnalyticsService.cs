using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexora.Core.Interfaces
{
    public interface IPaymentAnalyticsService
    {
        Task<PaymentMetrics> GetPaymentMetricsAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<RevenueAnalytics> GetRevenueAnalyticsAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<PaymentTrendAnalysis> GetPaymentTrendsAsync(int tenantId, DateTime startDate, DateTime endDate, string granularity = "daily");
        Task<PaymentMethodAnalytics> GetPaymentMethodAnalyticsAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<TransactionVolumeAnalytics> GetTransactionVolumeAnalyticsAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<RevenueForecast> GetRevenueForecastAsync(int tenantId, int forecastDays = 30);
        Task<PaymentFailureAnalysis> GetPaymentFailureAnalysisAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<CustomerPaymentBehavior> GetCustomerPaymentBehaviorAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<SmsUsageAnalytics> GetSmsUsageAnalyticsAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<CurrencyDistributionAnalytics> GetCurrencyDistributionAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<PaymentPerformanceReport> GeneratePaymentPerformanceReportAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<List<PaymentAnomalyAlert>> DetectPaymentAnomaliesAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<ComplianceReport> GenerateComplianceReportAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<RealTimeMetrics> GetRealTimeMetricsAsync(int tenantId);
        Task<PaymentReconciliationReport> GenerateReconciliationReportAsync(int tenantId, DateTime startDate, DateTime endDate);
    }

    public class PaymentMetrics
    {
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalRevenue { get; set; }
        public string Currency { get; set; }
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public int RefundedPayments { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public decimal TotalRefundAmount { get; set; }
        public decimal NetRevenue { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; }
    }

    public class RevenueAnalytics
    {
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RecurringRevenue { get; set; }
        public decimal OneTimeRevenue { get; set; }
        public decimal SmsRevenue { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal GrowthRate { get; set; }
        public List<DailyRevenue> DailyBreakdown { get; set; }
        public Dictionary<string, decimal> RevenueBySource { get; set; }
        public Dictionary<string, decimal> RevenueByCurrency { get; set; }
    }

    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageValue { get; set; }
    }

    public class PaymentTrendAnalysis
    {
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Granularity { get; set; }
        public List<TrendDataPoint> Volumetrend { get; set; }
        public List<TrendDataPoint> RevenueTrend { get; set; }
        public List<TrendDataPoint> SuccessRateTrend { get; set; }
        public TrendDirection OverallTrend { get; set; }
        public decimal TrendStrength { get; set; }
        public List<SeasonalPattern> SeasonalPatterns { get; set; }
    }

    public class TrendDataPoint
    {
        public DateTime Timestamp { get; set; }
        public decimal Value { get; set; }
        public int Count { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public enum TrendDirection
    {
        Increasing,
        Decreasing,
        Stable,
        Volatile
    }

    public class SeasonalPattern
    {
        public string PatternType { get; set; }
        public string Description { get; set; }
        public decimal Strength { get; set; }
        public Dictionary<string, decimal> PatternData { get; set; }
    }

    public class PaymentMethodAnalytics
    {
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<PaymentMethodStats> PaymentMethods { get; set; }
        public string MostPopularMethod { get; set; }
        public string HighestSuccessRateMethod { get; set; }
        public Dictionary<string, decimal> MethodPreferences { get; set; }
    }

    public class PaymentMethodStats
    {
        public string PaymentMethod { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal MarketShare { get; set; }
        public List<TrendDataPoint> Trend { get; set; }
    }

    public class TransactionVolumeAnalytics
    {
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalTransactions { get; set; }
        public decimal PeakHourVolume { get; set; }
        public DateTime PeakHour { get; set; }
        public List<HourlyVolume> HourlyDistribution { get; set; }
        public List<DayOfWeekVolume> WeeklyDistribution { get; set; }
        public Dictionary<string, int> VolumeByCountry { get; set; }
        public VolumeGrowthMetrics GrowthMetrics { get; set; }
    }

    public class HourlyVolume
    {
        public int Hour { get; set; }
        public int TransactionCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DayOfWeekVolume
    {
        public DayOfWeek DayOfWeek { get; set; }
        public int TransactionCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal AverageValue { get; set; }
    }

    public class VolumeGrowthMetrics
    {
        public decimal WeekOverWeekGrowth { get; set; }
        public decimal MonthOverMonthGrowth { get; set; }
        public decimal YearOverYearGrowth { get; set; }
        public decimal CompoundGrowthRate { get; set; }
    }

    public class RevenueForecast
    {
        public int TenantId { get; set; }
        public DateTime ForecastDate { get; set; }
        public int ForecastDays { get; set; }
        public List<ForecastDataPoint> Forecast { get; set; }
        public decimal PredictedRevenue { get; set; }
        public decimal ConfidenceInterval { get; set; }
        public string ForecastModel { get; set; }
        public Dictionary<string, object> ModelParameters { get; set; }
    }

    public class ForecastDataPoint
    {
        public DateTime Date { get; set; }
        public decimal PredictedRevenue { get; set; }
        public decimal LowerBound { get; set; }
        public decimal UpperBound { get; set; }
        public decimal Confidence { get; set; }
    }

    public class PaymentFailureAnalysis
    {
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalFailures { get; set; }
        public decimal FailureRate { get; set; }
        public Dictionary<string, int> FailureReasons { get; set; }
        public Dictionary<string, decimal> FailuresByPaymentMethod { get; set; }
        public List<FailureTrend> FailureTrends { get; set; }
        public List<string> RecommendedActions { get; set; }
        public decimal EstimatedLostRevenue { get; set; }
    }

    public class FailureTrend
    {
        public DateTime Date { get; set; }
        public int FailureCount { get; set; }
        public decimal FailureRate { get; set; }
        public string PrimaryReason { get; set; }
    }

    public class CustomerPaymentBehavior
    {
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int UniqueCustomers { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public decimal AverageTransactionsPerCustomer { get; set; }
        public List<CustomerSegment> CustomerSegments { get; set; }
        public Dictionary<string, decimal> PaymentFrequencyDistribution { get; set; }
        public List<RetentionMetric> RetentionMetrics { get; set; }
    }

    public class CustomerSegment
    {
        public string SegmentName { get; set; }
        public int CustomerCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageValue { get; set; }
        public decimal Percentage { get; set; }
        public Dictionary<string, object> Characteristics { get; set; }
    }

    public class RetentionMetric
    {
        public string Period { get; set; }
        public decimal RetentionRate { get; set; }
        public int RetainedCustomers { get; set; }
        public int NewCustomers { get; set; }
        public int ChurnedCustomers { get; set; }
    }

    public class SmsUsageAnalytics
    {
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalMessages { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageCostPerMessage { get; set; }
        public Dictionary<string, SmsCountryStats> UsageByCountry { get; set; }
        public Dictionary<string, SmsTypeStats> UsageByType { get; set; }
        public List<SmsDailyUsage> DailyUsage { get; set; }
        public SmsGrowthMetrics GrowthMetrics { get; set; }
        public List<SmsOptimizationRecommendation> OptimizationRecommendations { get; set; }
    }

    public class SmsCountryStats
    {
        public string Country { get; set; }
        public int MessageCount { get; set; }
        public decimal Cost { get; set; }
        public decimal CostPerMessage { get; set; }
        public decimal MarketShare { get; set; }
    }

    public class SmsTypeStats
    {
        public string MessageType { get; set; }
        public int MessageCount { get; set; }
        public decimal Cost { get; set; }
        public decimal CostPerMessage { get; set; }
        public decimal UsagePercentage { get; set; }
    }

    public class SmsDailyUsage
    {
        public DateTime Date { get; set; }
        public int MessageCount { get; set; }
        public decimal Cost { get; set; }
        public Dictionary<string, int> CountryBreakdown { get; set; }
    }

    public class SmsGrowthMetrics
    {
        public decimal MessageGrowthRate { get; set; }
        public decimal CostGrowthRate { get; set; }
        public decimal EfficiencyImprovement { get; set; }
    }

    public class SmsOptimizationRecommendation
    {
        public string RecommendationType { get; set; }
        public string Description { get; set; }
        public decimal PotentialSavings { get; set; }
        public string Priority { get; set; }
        public Dictionary<string, object> Details { get; set; }
    }

    public class CurrencyDistributionAnalytics
    {
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Dictionary<string, CurrencyStats> CurrencyBreakdown { get; set; }
        public string PrimaryCurrency { get; set; }
        public decimal CurrencyDiversificationIndex { get; set; }
        public List<CurrencyTrend> CurrencyTrends { get; set; }
        public Dictionary<string, decimal> ExchangeRateImpact { get; set; }
    }

    public class CurrencyStats
    {
        public string Currency { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public List<TrendDataPoint> Trend { get; set; }
    }

    public class CurrencyTrend
    {
        public string Currency { get; set; }
        public decimal GrowthRate { get; set; }
        public TrendDirection Direction { get; set; }
        public List<TrendDataPoint> DataPoints { get; set; }
    }

    public class PaymentPerformanceReport
    {
        public int TenantId { get; set; }
        public DateTime ReportDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public PaymentMetrics OverallMetrics { get; set; }
        public RevenueAnalytics RevenueAnalytics { get; set; }
        public PaymentMethodAnalytics PaymentMethodAnalytics { get; set; }
        public PaymentFailureAnalysis FailureAnalysis { get; set; }
        public List<KeyInsight> KeyInsights { get; set; }
        public List<ActionableRecommendation> Recommendations { get; set; }
        public Dictionary<string, object> CustomMetrics { get; set; }
    }

    public class KeyInsight
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Impact { get; set; }
        public Dictionary<string, object> SupportingData { get; set; }
    }

    public class ActionableRecommendation
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public decimal EstimatedImpact { get; set; }
        public string ImplementationEffort { get; set; }
        public List<string> ActionSteps { get; set; }
    }

    public class PaymentAnomalyAlert
    {
        public int TenantId { get; set; }
        public DateTime DetectedAt { get; set; }
        public string AnomalyType { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public decimal AnomalyScore { get; set; }
        public Dictionary<string, object> AnomalyData { get; set; }
        public List<string> RecommendedActions { get; set; }
        public bool IsResolved { get; set; }
    }

    public class ComplianceReport
    {
        public int TenantId { get; set; }
        public DateTime ReportDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ComplianceCheck> ComplianceChecks { get; set; }
        public decimal OverallComplianceScore { get; set; }
        public List<ComplianceViolation> Violations { get; set; }
        public List<ComplianceRecommendation> Recommendations { get; set; }
        public Dictionary<string, object> RegulatoryMetrics { get; set; }
    }

    public class ComplianceCheck
    {
        public string CheckName { get; set; }
        public string Category { get; set; }
        public bool Passed { get; set; }
        public decimal Score { get; set; }
        public string Description { get; set; }
        public List<string> Evidence { get; set; }
    }

    public class ComplianceViolation
    {
        public string ViolationType { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public DateTime DetectedAt { get; set; }
        public List<string> AffectedTransactions { get; set; }
        public string RemediationStatus { get; set; }
    }

    public class ComplianceRecommendation
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string RegulatoryBasis { get; set; }
        public List<string> ImplementationSteps { get; set; }
    }

    public class RealTimeMetrics
    {
        public int TenantId { get; set; }
        public DateTime Timestamp { get; set; }
        public int TransactionsLast24Hours { get; set; }
        public decimal RevenueLast24Hours { get; set; }
        public decimal CurrentSuccessRate { get; set; }
        public int ActiveSessions { get; set; }
        public decimal AverageProcessingTime { get; set; }
        public List<RealtimeAlert> ActiveAlerts { get; set; }
        public Dictionary<string, decimal> LiveMetrics { get; set; }
    }

    public class RealtimeAlert
    {
        public string AlertType { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
        public DateTime TriggeredAt { get; set; }
        public Dictionary<string, object> AlertData { get; set; }
    }

    public class PaymentReconciliationReport
    {
        public int TenantId { get; set; }
        public DateTime ReportDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalTransactions { get; set; }
        public int ReconciledTransactions { get; set; }
        public int UnreconciledTransactions { get; set; }
        public decimal ReconciliationRate { get; set; }
        public List<ReconciliationDiscrepancy> Discrepancies { get; set; }
        public decimal TotalDiscrepancyAmount { get; set; }
        public List<ReconciliationRecommendation> Recommendations { get; set; }
    }

    public class ReconciliationDiscrepancy
    {
        public string TransactionId { get; set; }
        public string DiscrepancyType { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Difference { get; set; }
        public string Status { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Description { get; set; }
    }

    public class ReconciliationRecommendation
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public List<string> ActionSteps { get; set; }
        public decimal EstimatedResolution { get; set; }
    }
}
