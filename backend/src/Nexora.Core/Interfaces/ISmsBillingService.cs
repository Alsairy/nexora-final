using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexora.Core.Interfaces
{
    public interface ISmsBillingService
    {
        Task<SmsBillingResult> CalculateUsageCostAsync(SmsUsageRequest request);
        Task<SmsBillingCycle> CreateBillingCycleAsync(CreateBillingCycleRequest request);
        Task<SmsBillingCycle> GetCurrentBillingCycleAsync(int tenantId);
        Task<List<SmsBillingCycle>> GetBillingHistoryAsync(int tenantId, DateTime? startDate = null, DateTime? endDate = null);
        Task<SmsUsageSummary> GetUsageSummaryAsync(int tenantId, DateTime startDate, DateTime endDate);
        Task<SmsBillingResult> ProcessBillingCycleAsync(int billingCycleId);
        Task<List<SmsRateCard>> GetRateCardAsync(string country = null);
        Task<SmsCostEstimate> EstimateCostAsync(SmsEstimateRequest request);
        Task<SmsBillingAlert> CheckBillingAlertsAsync(int tenantId);
        Task<SmsBillingResult> ApplyCreditsAsync(ApplyCreditsRequest request);
        Task<List<SmsTransaction>> GetTransactionHistoryAsync(int tenantId, DateTime? startDate = null, DateTime? endDate = null);
        Task<SmsBillingSettings> GetBillingSettingsAsync(int tenantId);
        Task<SmsBillingSettings> UpdateBillingSettingsAsync(int tenantId, UpdateBillingSettingsRequest request);
    }

    public class SmsUsageRequest
    {
        public int TenantId { get; set; }
        public int MessageCount { get; set; }
        public string Country { get; set; }
        public string MessageType { get; set; }
        public string Currency { get; set; }
        public DateTime UsageDate { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class SmsBillingResult
    {
        public bool Success { get; set; }
        public decimal TotalCost { get; set; }
        public string Currency { get; set; }
        public int MessageCount { get; set; }
        public decimal CostPerMessage { get; set; }
        public string BillingCycleId { get; set; }
        public string TransactionId { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class SmsBillingCycle
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public decimal TotalCost { get; set; }
        public string Currency { get; set; }
        public int TotalMessages { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class CreateBillingCycleRequest
    {
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Currency { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class SmsUsageSummary
    {
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalMessages { get; set; }
        public decimal TotalCost { get; set; }
        public string Currency { get; set; }
        public Dictionary<string, SmsCountryUsage> UsageByCountry { get; set; }
        public Dictionary<string, SmsTypeUsage> UsageByType { get; set; }
        public List<SmsDailyUsage> DailyUsage { get; set; }
    }

    public class SmsCountryUsage
    {
        public string Country { get; set; }
        public int MessageCount { get; set; }
        public decimal Cost { get; set; }
        public decimal CostPerMessage { get; set; }
    }

    public class SmsTypeUsage
    {
        public string MessageType { get; set; }
        public int MessageCount { get; set; }
        public decimal Cost { get; set; }
        public decimal CostPerMessage { get; set; }
    }

    public class SmsDailyUsage
    {
        public DateTime Date { get; set; }
        public int MessageCount { get; set; }
        public decimal Cost { get; set; }
    }

    public class SmsRateCard
    {
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public decimal StandardRate { get; set; }
        public decimal PremiumRate { get; set; }
        public string Currency { get; set; }
        public DateTime EffectiveDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class SmsEstimateRequest
    {
        public int MessageCount { get; set; }
        public string Country { get; set; }
        public string MessageType { get; set; }
        public string Currency { get; set; }
    }

    public class SmsCostEstimate
    {
        public int MessageCount { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Currency { get; set; }
        public decimal CostPerMessage { get; set; }
        public string Country { get; set; }
        public string MessageType { get; set; }
    }

    public class SmsBillingAlert
    {
        public int TenantId { get; set; }
        public string AlertType { get; set; }
        public string Message { get; set; }
        public decimal CurrentUsage { get; set; }
        public decimal Threshold { get; set; }
        public string Currency { get; set; }
        public DateTime AlertDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class ApplyCreditsRequest
    {
        public int TenantId { get; set; }
        public decimal CreditAmount { get; set; }
        public string Currency { get; set; }
        public string Reason { get; set; }
        public string ReferenceId { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class SmsTransaction
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string TransactionType { get; set; }
        public int MessageCount { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Country { get; set; }
        public string MessageType { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class SmsBillingSettings
    {
        public int TenantId { get; set; }
        public string BillingCycle { get; set; }
        public string Currency { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal AlertThreshold { get; set; }
        public bool AutoRecharge { get; set; }
        public decimal AutoRechargeAmount { get; set; }
        public string PaymentMethod { get; set; }
        public bool EnableAlerts { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class UpdateBillingSettingsRequest
    {
        public string BillingCycle { get; set; }
        public string Currency { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal? AlertThreshold { get; set; }
        public bool? AutoRecharge { get; set; }
        public decimal? AutoRechargeAmount { get; set; }
        public string PaymentMethod { get; set; }
        public bool? EnableAlerts { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
