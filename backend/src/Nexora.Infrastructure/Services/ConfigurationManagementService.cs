using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexora.Core.Configuration;
using Nexora.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Nexora.Infrastructure.Services
{
    public class ConfigurationManagementService : IConfigurationManagementService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigurationManagementService> _logger;
        private readonly ICacheService _cacheService;
        private readonly ITenantService _tenantService;
        private readonly FintechSettings _fintechSettings;

        public ConfigurationManagementService(
            IConfiguration configuration,
            ILogger<ConfigurationManagementService> logger,
            ICacheService cacheService,
            ITenantService tenantService,
            IOptions<FintechSettings> fintechSettings)
        {
            _configuration = configuration;
            _logger = logger;
            _cacheService = cacheService;
            _tenantService = tenantService;
            _fintechSettings = fintechSettings.Value;
        }

        public async Task<FintechSettings> GetFintechSettingsAsync(int? tenantId = null)
        {
            var cacheKey = GetCacheKey("fintech-settings", tenantId);
            
            var cachedSettings = await _cacheService.GetAsync<FintechSettings>(cacheKey);
            if (cachedSettings != null)
            {
                return cachedSettings;
            }

            var settings = await LoadFintechSettingsAsync(tenantId);
            await _cacheService.SetAsync(cacheKey, settings, TimeSpan.FromMinutes(30));
            
            return settings;
        }

        public async Task<PaymentProviderSettings> GetPaymentProviderSettingsAsync(int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            return fintechSettings.PaymentProviders;
        }

        public async Task<SmsPricingSettings> GetSmsPricingSettingsAsync(int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            return fintechSettings.SmsPricing;
        }

        public async Task<CurrencyExchangeSettings> GetCurrencyExchangeSettingsAsync(int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            return fintechSettings.CurrencyExchange;
        }

        public async Task<FraudDetectionSettings> GetFraudDetectionSettingsAsync(int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            return fintechSettings.FraudDetection;
        }

        public async Task<ComplianceSettings> GetComplianceSettingsAsync(int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            return fintechSettings.Compliance;
        }

        public async Task<BillingSettings> GetBillingSettingsAsync(int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            return fintechSettings.Billing;
        }

        public async Task UpdateFintechSettingsAsync(FintechSettings settings, int? tenantId = null)
        {
            if (!await ValidateConfigurationAsync(settings))
            {
                throw new ArgumentException("Invalid fintech settings configuration");
            }

            await SaveTenantConfigurationAsync("Fintech", settings, tenantId);
            await InvalidateCacheAsync("fintech-settings", tenantId);
            
            _logger.LogInformation("Updated fintech settings for tenant {TenantId}", tenantId ?? 0);
        }

        public async Task UpdatePaymentProviderSettingsAsync(PaymentProviderSettings settings, int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            fintechSettings.PaymentProviders = settings;
            await UpdateFintechSettingsAsync(fintechSettings, tenantId);
        }

        public async Task UpdateSmsPricingSettingsAsync(SmsPricingSettings settings, int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            fintechSettings.SmsPricing = settings;
            await UpdateFintechSettingsAsync(fintechSettings, tenantId);
        }

        public async Task UpdateCurrencyExchangeSettingsAsync(CurrencyExchangeSettings settings, int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            fintechSettings.CurrencyExchange = settings;
            await UpdateFintechSettingsAsync(fintechSettings, tenantId);
        }

        public async Task UpdateFraudDetectionSettingsAsync(FraudDetectionSettings settings, int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            fintechSettings.FraudDetection = settings;
            await UpdateFintechSettingsAsync(fintechSettings, tenantId);
        }

        public async Task UpdateComplianceSettingsAsync(ComplianceSettings settings, int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            fintechSettings.Compliance = settings;
            await UpdateFintechSettingsAsync(fintechSettings, tenantId);
        }

        public async Task UpdateBillingSettingsAsync(BillingSettings settings, int? tenantId = null)
        {
            var fintechSettings = await GetFintechSettingsAsync(tenantId);
            fintechSettings.Billing = settings;
            await UpdateFintechSettingsAsync(fintechSettings, tenantId);
        }

        public async Task<PaymentProviderConfig> GetPaymentProviderConfigAsync(string providerName, int? tenantId = null)
        {
            var paymentSettings = await GetPaymentProviderSettingsAsync(tenantId);
            
            if (paymentSettings.Providers.TryGetValue(providerName.ToLower(), out var config))
            {
                return config;
            }

            throw new ArgumentException($"Payment provider '{providerName}' not found");
        }

        public async Task<SmsPricingTier> GetSmsPricingTierAsync(int volume, int? tenantId = null)
        {
            var smsSettings = await GetSmsPricingSettingsAsync(tenantId);
            
            var applicableTier = smsSettings.PricingTiers.Values
                .Where(tier => volume >= tier.MinVolume && volume <= tier.MaxVolume)
                .OrderByDescending(tier => tier.MinVolume)
                .FirstOrDefault();

            if (applicableTier == null)
            {
                applicableTier = smsSettings.PricingTiers.Values
                    .OrderByDescending(tier => tier.MaxVolume)
                    .FirstOrDefault();
            }

            return applicableTier ?? throw new InvalidOperationException("No SMS pricing tier found for volume: " + volume);
        }

        public async Task<SmsCountryPricing> GetSmsCountryPricingAsync(string countryCode, int? tenantId = null)
        {
            var smsSettings = await GetSmsPricingSettingsAsync(tenantId);
            
            if (smsSettings.CountryPricing.TryGetValue(countryCode.ToUpper(), out var pricing))
            {
                return pricing;
            }

            return new SmsCountryPricing
            {
                CountryCode = countryCode.ToUpper(),
                CountryName = countryCode,
                BasePrice = 0.20m,
                Currency = smsSettings.DefaultCurrency,
                IsHighRisk = true,
                RiskMultiplier = 1.5m
            };
        }

        public async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency, int? tenantId = null)
        {
            if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return 1.0m;
            }

            var cacheKey = GetCacheKey($"exchange-rate-{fromCurrency}-{toCurrency}", tenantId);
            var cachedRate = await _cacheService.GetAsync<decimal?>(cacheKey);
            
            if (cachedRate.HasValue)
            {
                return cachedRate.Value;
            }

            var exchangeSettings = await GetCurrencyExchangeSettingsAsync(tenantId);
            var rate = await FetchExchangeRateAsync(fromCurrency, toCurrency, exchangeSettings);
            
            await _cacheService.SetAsync(cacheKey, rate, TimeSpan.FromMinutes(exchangeSettings.CacheExpirationMinutes));
            
            return rate;
        }

        public async Task<bool> ValidateConfigurationAsync(object configuration)
        {
            try
            {
                var validationContext = new ValidationContext(configuration);
                var validationResults = new List<ValidationResult>();
                
                var isValid = Validator.TryValidateObject(configuration, validationContext, validationResults, true);
                
                if (!isValid)
                {
                    _logger.LogWarning("Configuration validation failed: {Errors}", 
                        string.Join(", ", validationResults.Select(r => r.ErrorMessage)));
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetConfigurationSummaryAsync(int? tenantId = null)
        {
            var summary = new Dictionary<string, object>();
            
            try
            {
                var fintechSettings = await GetFintechSettingsAsync(tenantId);
                
                summary["PaymentProviders"] = new
                {
                    DefaultProvider = fintechSettings.PaymentProviders.DefaultProvider,
                    EnabledProviders = fintechSettings.PaymentProviders.Providers
                        .Where(p => p.Value.IsEnabled)
                        .Select(p => p.Key)
                        .ToList(),
                    FailoverEnabled = fintechSettings.PaymentProviders.EnableFailover
                };
                
                summary["SmsPricing"] = new
                {
                    DefaultCurrency = fintechSettings.SmsPricing.DefaultCurrency,
                    PricingTiers = fintechSettings.SmsPricing.PricingTiers.Count,
                    CountriesSupported = fintechSettings.SmsPricing.CountryPricing.Count,
                    DynamicPricingEnabled = fintechSettings.SmsPricing.EnableDynamicPricing
                };
                
                summary["FraudDetection"] = new
                {
                    RealTimeEnabled = fintechSettings.FraudDetection.EnableRealTimeDetection,
                    MachineLearningEnabled = fintechSettings.FraudDetection.EnableMachineLearning,
                    HighRiskThreshold = fintechSettings.FraudDetection.Thresholds.HighRiskThreshold,
                    BlockedCountries = fintechSettings.FraudDetection.Rules.Geographic.BlockedCountries.Count
                };
                
                summary["Compliance"] = new
                {
                    GdprEnabled = fintechSettings.Compliance.EnableGdprCompliance,
                    PciEnabled = fintechSettings.Compliance.EnablePciCompliance,
                    KsaEnabled = fintechSettings.Compliance.EnableKsaCompliance,
                    DataRetentionDays = fintechSettings.Compliance.DataRetentionDays
                };
                
                summary["CurrencyExchange"] = new
                {
                    PrimaryProvider = fintechSettings.CurrencyExchange.PrimaryProvider,
                    BaseCurrency = fintechSettings.CurrencyExchange.BaseCurrency,
                    SupportedCurrencies = fintechSettings.CurrencyExchange.SupportedCurrencies.Count,
                    RealTimeRatesEnabled = fintechSettings.CurrencyExchange.EnableRealTimeRates
                };
                
                summary["Billing"] = new
                {
                    DefaultCurrency = fintechSettings.Billing.DefaultCurrency,
                    DefaultBillingCycle = fintechSettings.Billing.DefaultBillingCycle.ToString(),
                    AutomaticBillingEnabled = fintechSettings.Billing.EnableAutomaticBilling,
                    VolumeDiscountsEnabled = fintechSettings.Billing.EnableVolumeDiscounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating configuration summary for tenant {TenantId}", tenantId);
                summary["Error"] = "Failed to generate configuration summary";
            }
            
            return summary;
        }

        public async Task ResetToDefaultsAsync(string configurationSection, int? tenantId = null)
        {
            try
            {
                switch (configurationSection.ToLower())
                {
                    case "paymentproviders":
                        var defaultPaymentSettings = _configuration.GetSection("Fintech:PaymentProviders").Get<PaymentProviderSettings>();
                        if (defaultPaymentSettings != null)
                        {
                            await UpdatePaymentProviderSettingsAsync(defaultPaymentSettings, tenantId);
                        }
                        break;
                        
                    case "smspricing":
                        var defaultSmsSettings = _configuration.GetSection("Fintech:SmsPricing").Get<SmsPricingSettings>();
                        if (defaultSmsSettings != null)
                        {
                            await UpdateSmsPricingSettingsAsync(defaultSmsSettings, tenantId);
                        }
                        break;
                        
                    case "frauddetection":
                        var defaultFraudSettings = _configuration.GetSection("Fintech:FraudDetection").Get<FraudDetectionSettings>();
                        if (defaultFraudSettings != null)
                        {
                            await UpdateFraudDetectionSettingsAsync(defaultFraudSettings, tenantId);
                        }
                        break;
                        
                    case "currencyexchange":
                        var defaultCurrencySettings = _configuration.GetSection("Fintech:CurrencyExchange").Get<CurrencyExchangeSettings>();
                        if (defaultCurrencySettings != null)
                        {
                            await UpdateCurrencyExchangeSettingsAsync(defaultCurrencySettings, tenantId);
                        }
                        break;
                        
                    case "compliance":
                        var defaultComplianceSettings = _configuration.GetSection("Fintech:Compliance").Get<ComplianceSettings>();
                        if (defaultComplianceSettings != null)
                        {
                            await UpdateComplianceSettingsAsync(defaultComplianceSettings, tenantId);
                        }
                        break;
                        
                    case "billing":
                        var defaultBillingSettings = _configuration.GetSection("Fintech:Billing").Get<BillingSettings>();
                        if (defaultBillingSettings != null)
                        {
                            await UpdateBillingSettingsAsync(defaultBillingSettings, tenantId);
                        }
                        break;
                        
                    default:
                        throw new ArgumentException($"Unknown configuration section: {configurationSection}");
                }
                
                _logger.LogInformation("Reset configuration section {Section} to defaults for tenant {TenantId}", 
                    configurationSection, tenantId ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting configuration section {Section} for tenant {TenantId}", 
                    configurationSection, tenantId);
                throw;
            }
        }

        public async Task<bool> IsConfigurationValidAsync(string configurationSection, int? tenantId = null)
        {
            try
            {
                switch (configurationSection.ToLower())
                {
                    case "paymentproviders":
                        var paymentSettings = await GetPaymentProviderSettingsAsync(tenantId);
                        return await ValidateConfigurationAsync(paymentSettings);
                        
                    case "smspricing":
                        var smsSettings = await GetSmsPricingSettingsAsync(tenantId);
                        return await ValidateConfigurationAsync(smsSettings);
                        
                    case "frauddetection":
                        var fraudSettings = await GetFraudDetectionSettingsAsync(tenantId);
                        return await ValidateConfigurationAsync(fraudSettings);
                        
                    case "currencyexchange":
                        var currencySettings = await GetCurrencyExchangeSettingsAsync(tenantId);
                        return await ValidateConfigurationAsync(currencySettings);
                        
                    case "compliance":
                        var complianceSettings = await GetComplianceSettingsAsync(tenantId);
                        return await ValidateConfigurationAsync(complianceSettings);
                        
                    case "billing":
                        var billingSettings = await GetBillingSettingsAsync(tenantId);
                        return await ValidateConfigurationAsync(billingSettings);
                        
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration section {Section} for tenant {TenantId}", 
                    configurationSection, tenantId);
                return false;
            }
        }

        private async Task<FintechSettings> LoadFintechSettingsAsync(int? tenantId = null)
        {
            try
            {
                var settings = new FintechSettings();
                
                _configuration.GetSection("Fintech").Bind(settings);
                
                if (tenantId.HasValue)
                {
                    var tenantSettings = await LoadTenantConfigurationAsync<FintechSettings>("Fintech", tenantId.Value);
                    if (tenantSettings != null)
                    {
                        MergeSettings(settings, tenantSettings);
                    }
                }
                
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading fintech settings for tenant {TenantId}", tenantId);
                
                var defaultSettings = new FintechSettings();
                _configuration.GetSection("Fintech").Bind(defaultSettings);
                return defaultSettings;
            }
        }

        private async Task<T?> LoadTenantConfigurationAsync<T>(string section, int tenantId) where T : class
        {
            try
            {
                var cacheKey = GetCacheKey($"tenant-config-{section}", tenantId);
                var cachedConfig = await _cacheService.GetAsync<T>(cacheKey);
                
                if (cachedConfig != null)
                {
                    return cachedConfig;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tenant configuration for section {Section}, tenant {TenantId}", 
                    section, tenantId);
                return null;
            }
        }

        private async Task SaveTenantConfigurationAsync<T>(string section, T configuration, int? tenantId) where T : class
        {
            try
            {
                if (!tenantId.HasValue)
                {
                    _logger.LogWarning("Attempted to save tenant configuration without tenant ID for section {Section}", section);
                    return;
                }
                
                var cacheKey = GetCacheKey($"tenant-config-{section}", tenantId);
                await _cacheService.SetAsync(cacheKey, configuration, TimeSpan.FromHours(24));
                
                _logger.LogInformation("Saved tenant configuration for section {Section}, tenant {TenantId}", 
                    section, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving tenant configuration for section {Section}, tenant {TenantId}", 
                    section, tenantId);
                throw;
            }
        }

        private void MergeSettings<T>(T target, T source) where T : class
        {
            try
            {
                var properties = typeof(T).GetProperties();
                foreach (var property in properties)
                {
                    if (property.CanWrite)
                    {
                        var sourceValue = property.GetValue(source);
                        if (sourceValue != null)
                        {
                            property.SetValue(target, sourceValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging settings of type {Type}", typeof(T).Name);
            }
        }

        private async Task<decimal> FetchExchangeRateAsync(string fromCurrency, string toCurrency, CurrencyExchangeSettings settings)
        {
            try
            {
                if (settings.Providers.TryGetValue(settings.PrimaryProvider, out var primaryProvider) && primaryProvider.IsEnabled)
                {
                    var rate = await FetchRateFromProviderAsync(fromCurrency, toCurrency, primaryProvider);
                    if (rate > 0)
                    {
                        return rate;
                    }
                }
                
                foreach (var fallbackProviderName in settings.FallbackProviders)
                {
                    if (settings.Providers.TryGetValue(fallbackProviderName, out var fallbackProvider) && fallbackProvider.IsEnabled)
                    {
                        var rate = await FetchRateFromProviderAsync(fromCurrency, toCurrency, fallbackProvider);
                        if (rate > 0)
                        {
                            return rate;
                        }
                    }
                }
                
                _logger.LogWarning("All exchange rate providers failed for {FromCurrency} to {ToCurrency}, using default rate", 
                    fromCurrency, toCurrency);
                return 1.0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching exchange rate from {FromCurrency} to {ToCurrency}", 
                    fromCurrency, toCurrency);
                return 1.0m;
            }
        }

        private async Task<decimal> FetchRateFromProviderAsync(string fromCurrency, string toCurrency, CurrencyExchangeProvider provider)
        {
            try
            {
                await Task.Delay(100); // Simulate API call
                
                var mockRates = new Dictionary<string, decimal>
                {
                    ["USD-SAR"] = 3.75m,
                    ["EUR-SAR"] = 4.10m,
                    ["GBP-SAR"] = 4.75m,
                    ["AED-SAR"] = 1.02m,
                    ["KWD-SAR"] = 12.25m,
                    ["QAR-SAR"] = 1.03m,
                    ["BHD-SAR"] = 9.95m,
                    ["OMR-SAR"] = 9.75m
                };
                
                var rateKey = $"{fromCurrency}-{toCurrency}";
                if (mockRates.TryGetValue(rateKey, out var rate))
                {
                    return rate;
                }
                
                var reverseKey = $"{toCurrency}-{fromCurrency}";
                if (mockRates.TryGetValue(reverseKey, out var reverseRate))
                {
                    return 1.0m / reverseRate;
                }
                
                return 1.0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching rate from provider {Provider} for {FromCurrency} to {ToCurrency}", 
                    provider.Name, fromCurrency, toCurrency);
                return 0;
            }
        }

        private string GetCacheKey(string key, int? tenantId)
        {
            return tenantId.HasValue ? $"{key}:tenant:{tenantId}" : $"{key}:global";
        }

        private async Task InvalidateCacheAsync(string key, int? tenantId)
        {
            var cacheKey = GetCacheKey(key, tenantId);
            await _cacheService.RemoveAsync(cacheKey);
        }
    }
}
