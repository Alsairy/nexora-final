using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Core.Interfaces;
using Nexora.Web.Api;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Nexora.Tests.Integration
{
    public class WebhookIntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly HttpClient _client;

        public WebhookIntegrationTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task PaymentWebhook_ShouldReturn200_WithValidPayload()
        {
            var provider = "stripe";
            var payload = new
            {
                event_type = "payment.succeeded",
                payment_id = "pi_test_123",
                amount = 1000,
                currency = "usd"
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            content.Headers.Add("X-Signature", "test_signature");

            var response = await _client.PostAsync($"/api/v1/webhooks/payment/{provider}", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SmsWebhook_ShouldReturn200_WithValidPayload()
        {
            var provider = "twilio";
            var payload = new
            {
                message_id = "msg_test_123",
                status = "delivered",
                timestamp = DateTime.UtcNow
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            content.Headers.Add("X-Signature", "test_signature");

            var response = await _client.PostAsync($"/api/v1/webhooks/sms/{provider}", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetWebhookLogs_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/webhooks/logs");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetWebhookConfiguration_ShouldRequireAuthentication()
        {
            var response = await _client.GetAsync("/api/v1/webhooks/configuration");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateWebhookConfiguration_ShouldRequireAuthentication()
        {
            var configuration = new
            {
                tenantId = 1,
                endpoints = new Dictionary<string, object>(),
                settings = new { enableRetries = true }
            };

            var jsonPayload = JsonSerializer.Serialize(configuration);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _client.PutAsync("/api/v1/webhooks/configuration", content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RetryFailedWebhook_ShouldRequireAuthentication()
        {
            var webhookLogId = 1;

            var response = await _client.PostAsync($"/api/v1/webhooks/retry/{webhookLogId}", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetWebhookDeliveryStatus_ShouldRequireAuthentication()
        {
            var webhookId = "test_webhook_id";

            var response = await _client.GetAsync($"/api/v1/webhooks/status/{webhookId}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SendWebhook_ShouldRequireAuthentication()
        {
            var request = new
            {
                url = "https://example.com/webhook",
                payload = new { test = "data" },
                secret = "test_secret",
                maxRetries = 3
            };

            var jsonPayload = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/v1/webhooks/send", content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
