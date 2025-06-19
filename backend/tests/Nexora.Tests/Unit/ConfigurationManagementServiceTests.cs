using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nexora.Core.Configuration;
using Nexora.Core.Interfaces;
using Nexora.Infrastructure.Services;
using Xunit;

namespace Nexora.Tests.Unit
{
    public class ConfigurationManagementServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<ConfigurationManagementService>> _mockLogger;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ITenantService> _mockTenantService;
        private readonly Mock<IOptions<FintechSettings>> _mockFintechSettings;
        private readonly ConfigurationManagementService _service;

        public ConfigurationManagementServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<ConfigurationManagementService>>();
            _mockCacheService = new Mock<ICacheService>();
            _mockTenantService = new Mock<ITenantService>();
            _mockFintechSettings = new Mock<IOptions<FintechSettings>>();

            var fintechSettings = new FintechSettings
            {
                PaymentProviders = new PaymentProviderSettings
                {
                    DefaultProvider = "stripe",
                    EnableFailover = true,
                    Providers = new Dictionary<string, PaymentProviderConfig>
                    {
                        ["stripe"] = new PaymentProviderConfig
                        {
                            Name = "Stripe",
                            IsEnabled = true,
                            ApiKey = "sk_test_123",
                            WebhookSecret = "whsec_123",
                            SupportedCurrencies = new List<string> { "USD", "SAR" },
                            TransactionFeePercentage = 2.9m,
                            FixedFee = 0.30m
                        }
                    }
                },
                SmsPricing = new SmsPricingSettings
                {
                    DefaultCurrency = "SAR",
                    EnableDynamicPricing = true,
                    PricingTiers = new Dictionary<string, SmsPricingTier>
                    {
                        ["starter"] = new SmsPricingTier
                        {
                            Name = "Starter",
                            MinVolume = 1,
                            MaxVolume = 1000,
                            PricePerMessage = 0.15m,
                            Currency = "SAR"
                        }
                    }
                }
            };

            _mockFintechSettings.Setup(x => x.Value).Returns(fintechSettings);

            _service = new ConfigurationManagementService(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockCacheService.Object,
                _mockTenantService.Object,
                _mockFintechSettings.Object);
        }

        [Fact]
        public async Task GetFintechSettingsAsync_ShouldReturnCachedSettings_WhenCacheExists()
        {
            var cachedSettings = new FintechSettings();
            _mockCacheService.Setup(x => x.GetAsync<FintechSettings>(It.IsAny<string>()))
                .ReturnsAsync(cachedSettings);

            var result = await _service.GetFintechSettingsAsync();

            Assert.NotNull(result);
            Assert.Equal(cachedSettings, result);
            _mockCacheService.Verify(x => x.GetAsync<FintechSettings>(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetPaymentProviderConfigAsync_ShouldReturnConfig_WhenProviderExists()
        {
            var providerName = "stripe";
            _mockCacheService.Setup(x => x.GetAsync<FintechSettings>(It.IsAny<string>()))
                .ReturnsAsync((FintechSettings)null);

            var result = await _service.GetPaymentProviderConfigAsync(providerName);

            Assert.NotNull(result);
            Assert.Equal("Stripe", result.Name);
            Assert.True(result.IsEnabled);
        }

        [Fact]
        public async Task GetPaymentProviderConfigAsync_ShouldThrowException_WhenProviderNotFound()
        {
            var providerName = "nonexistent";
            _mockCacheService.Setup(x => x.GetAsync<FintechSettings>(It.IsAny<string>()))
                .ReturnsAsync((FintechSettings)null);

            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.GetPaymentProviderConfigAsync(providerName));
        }

        [Fact]
        public async Task GetSmsPricingTierAsync_ShouldReturnCorrectTier_ForGivenVolume()
        {
            var volume = 500;
            _mockCacheService.Setup(x => x.GetAsync<FintechSettings>(It.IsAny<string>()))
                .ReturnsAsync((FintechSettings)null);

            var result = await _service.GetSmsPricingTierAsync(volume);

            Assert.NotNull(result);
            Assert.Equal("Starter", result.Name);
            Assert.True(volume >= result.MinVolume && volume <= result.MaxVolume);
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldReturnTrue_ForValidConfiguration()
        {
            var validConfig = new PaymentProviderSettings
            {
                DefaultProvider = "stripe",
                EnableFailover = true
            };

            var result = await _service.ValidateConfigurationAsync(validConfig);

            Assert.True(result);
        }

        [Fact]
        public async Task GetExchangeRateAsync_ShouldReturnOne_ForSameCurrency()
        {
            var fromCurrency = "USD";
            var toCurrency = "USD";

            var result = await _service.GetExchangeRateAsync(fromCurrency, toCurrency);

            Assert.Equal(1.0m, result);
        }

        [Fact]
        public async Task UpdateFintechSettingsAsync_ShouldInvalidateCache_AfterUpdate()
        {
            var settings = new FintechSettings();
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _service.UpdateFintechSettingsAsync(settings);

            _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Once);
        }
    }
}
