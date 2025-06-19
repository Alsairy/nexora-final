using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Configuration
{
    public class FintechSettings
    {
        public PaymentProviderSettings PaymentProviders { get; set; } = new();
        public SmsPricingSettings SmsPricing { get; set; } = new();
        public CurrencyExchangeSettings CurrencyExchange { get; set; } = new();
        public FraudDetectionSettings FraudDetection { get; set; } = new();
        public ComplianceSettings Compliance { get; set; } = new();
        public BillingSettings Billing { get; set; } = new();
    }

    public class PaymentProviderSettings
    {
        public Dictionary<string, PaymentProviderConfig> Providers { get; set; } = new();
        public string DefaultProvider { get; set; } = "stripe";
        public bool EnableFailover { get; set; } = true;
        public int FailoverTimeoutSeconds { get; set; } = 30;
        public bool EnableLoadBalancing { get; set; } = false;
        public Dictionary<string, decimal> ProviderWeights { get; set; } = new();
    }

    public class PaymentProviderConfig
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string ApiKey { get; set; } = string.Empty;
        
        public string ApiSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public bool IsTestMode { get; set; } = false;
        public decimal MaxTransactionAmount { get; set; } = 100000;
        public decimal MinTransactionAmount { get; set; } = 1;
        public List<string> SupportedCurrencies { get; set; } = new();
        public List<string> SupportedCountries { get; set; } = new();
        public Dictionary<string, string> CustomSettings { get; set; } = new();
        public PaymentProviderFees Fees { get; set; } = new();
    }

    public class PaymentProviderFees
    {
        public decimal FixedFee { get; set; } = 0;
        public decimal PercentageFee { get; set; } = 0;
        public decimal InternationalFee { get; set; } = 0;
        public Dictionary<string, decimal> CurrencyFees { get; set; } = new();
        public Dictionary<string, decimal> PaymentMethodFees { get; set; } = new();
    }

    public class SmsPricingSettings
    {
        public Dictionary<string, SmsPricingTier> PricingTiers { get; set; } = new();
        public Dictionary<string, SmsCountryPricing> CountryPricing { get; set; } = new();
        public Dictionary<string, SmsOperatorPricing> OperatorPricing { get; set; } = new();
        public SmsVolumeDiscounts VolumeDiscounts { get; set; } = new();
        public string DefaultCurrency { get; set; } = "SAR";
        public bool EnableDynamicPricing { get; set; } = false;
        public decimal MarkupPercentage { get; set; } = 10;
    }

    public class SmsPricingTier
    {
        public string Name { get; set; } = string.Empty;
        public int MinVolume { get; set; }
        public int MaxVolume { get; set; }
        public decimal PricePerMessage { get; set; }
        public string Currency { get; set; } = "SAR";
        public Dictionary<string, decimal> MessageTypePricing { get; set; } = new();
        public Dictionary<string, decimal> ChannelPricing { get; set; } = new();
    }

    public class SmsCountryPricing
    {
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Currency { get; set; } = "SAR";
        public Dictionary<string, decimal> OperatorPrices { get; set; } = new();
        public bool IsHighRisk { get; set; } = false;
        public decimal RiskMultiplier { get; set; } = 1.0m;
    }

    public class SmsOperatorPricing
    {
        public string OperatorCode { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "SAR";
        public bool IsPreferred { get; set; } = false;
        public decimal QualityScore { get; set; } = 1.0m;
        public Dictionary<string, decimal> MessageTypePricing { get; set; } = new();
    }

    public class SmsVolumeDiscounts
    {
        public List<VolumeDiscountTier> Tiers { get; set; } = new();
        public bool EnableAutoUpgrade { get; set; } = true;
        public int EvaluationPeriodDays { get; set; } = 30;
    }

    public class VolumeDiscountTier
    {
        public int MinVolume { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class CurrencyExchangeSettings
    {
        public string PrimaryProvider { get; set; } = "exchangerate-api";
        public List<string> FallbackProviders { get; set; } = new();
        public Dictionary<string, CurrencyExchangeProvider> Providers { get; set; } = new();
        public int CacheExpirationMinutes { get; set; } = 60;
        public decimal MaxExchangeRateAge { get; set; } = 24; // hours
        public bool EnableRealTimeRates { get; set; } = true;
        public List<string> SupportedCurrencies { get; set; } = new();
        public string BaseCurrency { get; set; } = "SAR";
    }

    public class CurrencyExchangeProvider
    {
        public string Name { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 1;
        public decimal RateLimit { get; set; } = 1000; // requests per hour
        public Dictionary<string, string> CustomHeaders { get; set; } = new();
    }

    public class FraudDetectionSettings
    {
        public bool EnableRealTimeDetection { get; set; } = true;
        public bool EnableMachineLearning { get; set; } = true;
        public int ModelUpdateIntervalHours { get; set; } = 24;
        public FraudThresholds Thresholds { get; set; } = new();
        public FraudRules Rules { get; set; } = new();
        public FraudNotificationSettings Notifications { get; set; } = new();
        public Dictionary<string, decimal> RiskWeights { get; set; } = new();
    }

    public class FraudThresholds
    {
        public decimal HighRiskThreshold { get; set; } = 0.8m;
        public decimal MediumRiskThreshold { get; set; } = 0.4m;
        public decimal LowRiskThreshold { get; set; } = 0.2m;
        public decimal AutoBlockThreshold { get; set; } = 0.95m;
        public decimal ManualReviewThreshold { get; set; } = 0.7m;
        public Dictionary<string, decimal> CountryRiskThresholds { get; set; } = new();
        public Dictionary<string, decimal> PaymentMethodThresholds { get; set; } = new();
    }

    public class FraudRules
    {
        public VelocityRules Velocity { get; set; } = new();
        public GeographicRules Geographic { get; set; } = new();
        public BehavioralRules Behavioral { get; set; } = new();
        public DeviceRules Device { get; set; } = new();
    }

    public class VelocityRules
    {
        public int MaxTransactionsPerHour { get; set; } = 10;
        public decimal MaxAmountPerHour { get; set; } = 50000;
        public int MaxTransactionsPerDay { get; set; } = 50;
        public decimal MaxAmountPerDay { get; set; } = 200000;
        public int MaxFailedAttemptsPerHour { get; set; } = 5;
        public Dictionary<string, VelocityLimit> PaymentMethodLimits { get; set; } = new();
    }

    public class VelocityLimit
    {
        public int MaxTransactions { get; set; }
        public decimal MaxAmount { get; set; }
        public int TimeWindowMinutes { get; set; }
    }

    public class GeographicRules
    {
        public List<string> BlockedCountries { get; set; } = new();
        public List<string> HighRiskCountries { get; set; } = new();
        public bool EnableVpnDetection { get; set; } = true;
        public bool BlockTorTraffic { get; set; } = true;
        public decimal CountryMismatchRiskScore { get; set; } = 0.3m;
    }

    public class BehavioralRules
    {
        public bool EnableUserBehaviorAnalysis { get; set; } = true;
        public decimal UnusualPatternThreshold { get; set; } = 0.5m;
        public int MinHistoryDays { get; set; } = 7;
        public Dictionary<string, decimal> BehaviorWeights { get; set; } = new();
    }

    public class DeviceRules
    {
        public bool EnableDeviceFingerprinting { get; set; } = true;
        public bool BlockEmulators { get; set; } = true;
        public bool RequireDeviceRegistration { get; set; } = false;
        public decimal NewDeviceRiskScore { get; set; } = 0.2m;
        public int MaxDevicesPerUser { get; set; } = 5;
    }

    public class FraudNotificationSettings
    {
        public bool EnableEmailNotifications { get; set; } = true;
        public bool EnableSmsNotifications { get; set; } = false;
        public bool EnableWebhookNotifications { get; set; } = true;
        public List<string> NotificationEmails { get; set; } = new();
        public string WebhookUrl { get; set; } = string.Empty;
        public Dictionary<string, bool> NotificationTypes { get; set; } = new();
    }

    public class ComplianceSettings
    {
        public bool EnableGdprCompliance { get; set; } = true;
        public bool EnablePciCompliance { get; set; } = true;
        public bool EnableKsaCompliance { get; set; } = true;
        public int DataRetentionDays { get; set; } = 2555; // 7 years
        public bool EnableAuditLogging { get; set; } = true;
        public bool RequireConsentForAnalytics { get; set; } = true;
        public Dictionary<string, ComplianceRule> Rules { get; set; } = new();
    }

    public class ComplianceRule
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string Jurisdiction { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class BillingSettings
    {
        public string DefaultCurrency { get; set; } = "SAR";
        public BillingCycle DefaultBillingCycle { get; set; } = BillingCycle.Monthly;
        public bool EnableAutomaticBilling { get; set; } = true;
        public int PaymentTermsDays { get; set; } = 30;
        public decimal LateFeePercentage { get; set; } = 5;
        public bool EnableVolumeDiscounts { get; set; } = true;
        public Dictionary<string, decimal> TaxRates { get; set; } = new();
        public InvoiceSettings Invoice { get; set; } = new();
    }

    public class InvoiceSettings
    {
        public string CompanyName { get; set; } = "Nexora Platform";
        public string CompanyAddress { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public bool EnableWhiteLabel { get; set; } = false;
        public Dictionary<string, string> CustomFields { get; set; } = new();
    }

    public enum BillingCycle
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Annually
    }
}
