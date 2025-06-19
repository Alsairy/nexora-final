using Nexora.Core.Configuration;

namespace Nexora.Core.Interfaces
{
    public interface IConfigurationManagementService
    {
        Task<FintechSettings> GetFintechSettingsAsync(int? tenantId = null);
        Task<PaymentProviderSettings> GetPaymentProviderSettingsAsync(int? tenantId = null);
        Task<SmsPricingSettings> GetSmsPricingSettingsAsync(int? tenantId = null);
        Task<CurrencyExchangeSettings> GetCurrencyExchangeSettingsAsync(int? tenantId = null);
        Task<FraudDetectionSettings> GetFraudDetectionSettingsAsync(int? tenantId = null);
        Task<ComplianceSettings> GetComplianceSettingsAsync(int? tenantId = null);
        Task<BillingSettings> GetBillingSettingsAsync(int? tenantId = null);

        Task UpdateFintechSettingsAsync(FintechSettings settings, int? tenantId = null);
        Task UpdatePaymentProviderSettingsAsync(PaymentProviderSettings settings, int? tenantId = null);
        Task UpdateSmsPricingSettingsAsync(SmsPricingSettings settings, int? tenantId = null);
        Task UpdateCurrencyExchangeSettingsAsync(CurrencyExchangeSettings settings, int? tenantId = null);
        Task UpdateFraudDetectionSettingsAsync(FraudDetectionSettings settings, int? tenantId = null);
        Task UpdateComplianceSettingsAsync(ComplianceSettings settings, int? tenantId = null);
        Task UpdateBillingSettingsAsync(BillingSettings settings, int? tenantId = null);

        Task<PaymentProviderConfig> GetPaymentProviderConfigAsync(string providerName, int? tenantId = null);
        Task<SmsPricingTier> GetSmsPricingTierAsync(int volume, int? tenantId = null);
        Task<SmsCountryPricing> GetSmsCountryPricingAsync(string countryCode, int? tenantId = null);
        Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency, int? tenantId = null);

        Task<bool> ValidateConfigurationAsync(object configuration);
        Task<Dictionary<string, object>> GetConfigurationSummaryAsync(int? tenantId = null);
        Task ResetToDefaultsAsync(string configurationSection, int? tenantId = null);
        Task<bool> IsConfigurationValidAsync(string configurationSection, int? tenantId = null);
    }
}
