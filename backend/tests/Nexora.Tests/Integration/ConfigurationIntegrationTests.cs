using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Nexora.Web.Api;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Nexora.Tests.Integration
{
    public class ConfigurationIntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly HttpClient _client;

        public ConfigurationIntegrationTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetConfigurationSummary_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/configuration/summary");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetFintechSettings_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/configuration/fintech");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateFintechSettings_ShouldRequireAuthentication()
        {
            var settings = new
            {
                paymentProviders = new
                {
                    defaultProvider = "stripe",
                    enableFailover = true
                }
            };

            var jsonPayload = JsonSerializer.Serialize(settings);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _client.PutAsync("/api/v1/configuration/fintech", content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetPaymentProviderSettings_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/configuration/payment-providers");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetPaymentProviderConfig_ShouldRequireAuthentication()
        {
            var providerName = "stripe";

            var response = await _client.GetAsync($"/api/v1/configuration/payment-providers/{providerName}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetSmsPricingSettings_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/configuration/sms-pricing");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetSmsPricingTier_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/configuration/sms-pricing/tier?volume=1000");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetSmsCountryPricing_ShouldRequireAuthentication()
        {
            var countryCode = "SA";

            var response = await _client.GetAsync($"/api/v1/configuration/sms-pricing/country/{countryCode}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrencyExchangeSettings_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/configuration/currency-exchange");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetExchangeRate_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/configuration/exchange-rate?fromCurrency=USD&toCurrency=SAR");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetFraudDetectionSettings_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/configuration/fraud-detection");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetComplianceSettings_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/configuration/compliance");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetBillingSettings_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/configuration/billing");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ValidateConfiguration_ShouldRequireAuthentication()
        {
            var section = "paymentproviders";

            var response = await _client.PostAsync($"/api/v1/configuration/validate/{section}", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ResetToDefaults_ShouldRequireAuthentication()
        {
            var section = "paymentproviders";

            var response = await _client.PostAsync($"/api/v1/configuration/reset/{section}", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
