using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Nexora.Core.Configuration;
using Nexora.Core.Interfaces;
using Nexora.Infrastructure.Services;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Nexora.Tests.Unit
{
    public class WebhookServiceTests
    {
        private readonly Mock<ILogger<WebhookService>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ITenantService> _mockTenantService;
        private readonly Mock<IPaymentService> _mockPaymentService;
        private readonly Mock<ISmsBillingService> _mockSmsBillingService;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<IOptions<FintechSettings>> _mockFintechSettings;
        private readonly WebhookService _service;

        public WebhookServiceTests()
        {
            _mockLogger = new Mock<ILogger<WebhookService>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockCacheService = new Mock<ICacheService>();
            _mockTenantService = new Mock<ITenantService>();
            _mockPaymentService = new Mock<IPaymentService>();
            _mockSmsBillingService = new Mock<ISmsBillingService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockFintechSettings = new Mock<IOptions<FintechSettings>>();

            _mockFintechSettings.Setup(x => x.Value).Returns(new FintechSettings());

            _service = new WebhookService(
                _mockLogger.Object,
                _httpClient,
                _mockCacheService.Object,
                _mockTenantService.Object,
                _mockPaymentService.Object,
                _mockSmsBillingService.Object,
                _mockNotificationService.Object,
                _mockFintechSettings.Object);
        }

        [Fact]
        public async Task ProcessPaymentWebhookAsync_ShouldReturnFalse_WhenNoConfigurationFound()
        {
            var provider = "unknown_provider";
            var payload = "{}";
            var signature = "test_signature";

            _mockCacheService.Setup(x => x.GetAsync<WebhookConfiguration>(It.IsAny<string>()))
                .ReturnsAsync(new WebhookConfiguration { Endpoints = new Dictionary<string, WebhookEndpoint>() });

            var result = await _service.ProcessPaymentWebhookAsync(provider, payload, signature);

            Assert.False(result);
        }

        [Fact]
        public async Task ProcessPaymentWebhookAsync_ShouldReturnFalse_WhenSignatureInvalid()
        {
            var provider = "stripe";
            var payload = "{}";
            var signature = "invalid_signature";

            var webhookConfig = new WebhookConfiguration
            {
                Endpoints = new Dictionary<string, WebhookEndpoint>
                {
                    [$"payment_{provider}"] = new WebhookEndpoint
                    {
                        Secret = "test_secret",
                        IsEnabled = true
                    }
                }
            };

            _mockCacheService.Setup(x => x.GetAsync<WebhookConfiguration>(It.IsAny<string>()))
                .ReturnsAsync(webhookConfig);

            var result = await _service.ProcessPaymentWebhookAsync(provider, payload, signature);

            Assert.False(result);
        }

        [Fact]
        public async Task ValidateWebhookSignatureAsync_ShouldReturnFalse_WhenSignatureEmpty()
        {
            var provider = "stripe";
            var payload = "test_payload";
            var signature = "";
            var secret = "test_secret";

            var result = await _service.ValidateWebhookSignatureAsync(provider, payload, signature, secret);

            Assert.False(result.IsValid);
            Assert.Equal("Missing signature or secret", result.Message);
        }

        [Fact]
        public async Task ValidateWebhookSignatureAsync_ShouldReturnFalse_WhenSecretEmpty()
        {
            var provider = "stripe";
            var payload = "test_payload";
            var signature = "test_signature";
            var secret = "";

            var result = await _service.ValidateWebhookSignatureAsync(provider, payload, signature, secret);

            Assert.False(result.IsValid);
            Assert.Equal("Missing signature or secret", result.Message);
        }

        [Fact]
        public async Task SendWebhookAsync_ShouldReturnTrue_WhenHttpRequestSucceeds()
        {
            var url = "https://example.com/webhook";
            var payload = new { test = "data" };
            var secret = "test_secret";

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var result = await _service.SendWebhookAsync(url, payload, secret, 1);

            Assert.True(result);
        }

        [Fact]
        public async Task SendWebhookAsync_ShouldReturnFalse_WhenAllRetriesFail()
        {
            var url = "https://example.com/webhook";
            var payload = new { test = "data" };
            var secret = "test_secret";

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var result = await _service.SendWebhookAsync(url, payload, secret, 2);

            Assert.False(result);
        }

        [Fact]
        public async Task GetWebhookConfigurationAsync_ShouldReturnCachedConfig_WhenCacheExists()
        {
            var cachedConfig = new WebhookConfiguration { TenantId = 1 };
            _mockCacheService.Setup(x => x.GetAsync<WebhookConfiguration>(It.IsAny<string>()))
                .ReturnsAsync(cachedConfig);

            var result = await _service.GetWebhookConfigurationAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.TenantId);
            _mockCacheService.Verify(x => x.GetAsync<WebhookConfiguration>(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateWebhookConfigurationAsync_ShouldUpdateCache()
        {
            var configuration = new WebhookConfiguration { TenantId = 1 };
            _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<WebhookConfiguration>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            await _service.UpdateWebhookConfigurationAsync(configuration, 1);

            _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<WebhookConfiguration>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task GetWebhookDeliveryStatusAsync_ShouldReturnDelivered()
        {
            var webhookId = "test_webhook_id";

            var result = await _service.GetWebhookDeliveryStatusAsync(webhookId);

            Assert.Equal(WebhookDeliveryStatus.Delivered, result);
        }

        [Fact]
        public async Task RetryFailedWebhookAsync_ShouldReturnTrue()
        {
            var webhookLogId = 1;

            var result = await _service.RetryFailedWebhookAsync(webhookLogId);

            Assert.True(result);
        }
    }
}
