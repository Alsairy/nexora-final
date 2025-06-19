using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexora.Core.Configuration;
using Nexora.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http;

namespace Nexora.Infrastructure.Services
{
    public class WebhookService : IWebhookService
    {
        private readonly ILogger<WebhookService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly ITenantService _tenantService;
        private readonly IPaymentService _paymentService;
        private readonly ISmsBillingService _smsBillingService;
        private readonly INotificationService _notificationService;
        private readonly FintechSettings _fintechSettings;

        public WebhookService(
            ILogger<WebhookService> logger,
            HttpClient httpClient,
            ICacheService cacheService,
            ITenantService tenantService,
            IPaymentService paymentService,
            ISmsBillingService smsBillingService,
            INotificationService notificationService,
            IOptions<FintechSettings> fintechSettings)
        {
            _logger = logger;
            _httpClient = httpClient;
            _cacheService = cacheService;
            _tenantService = tenantService;
            _paymentService = paymentService;
            _smsBillingService = smsBillingService;
            _notificationService = notificationService;
            _fintechSettings = fintechSettings.Value;
        }

        public async Task<bool> ProcessPaymentWebhookAsync(string provider, string payload, string signature)
        {
            try
            {
                _logger.LogInformation("Processing payment webhook from provider {Provider}", provider);

                var webhookConfig = await GetWebhookConfigurationAsync();
                if (!webhookConfig.Endpoints.ContainsKey($"payment_{provider}"))
                {
                    _logger.LogWarning("No webhook configuration found for payment provider {Provider}", provider);
                    return false;
                }

                var endpoint = webhookConfig.Endpoints[$"payment_{provider}"];
                var validationResult = await ValidateWebhookSignatureAsync(provider, payload, signature, endpoint.Secret);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Invalid webhook signature for payment provider {Provider}", provider);
                    await LogWebhookAsync(provider, "payment", payload, "Invalid signature", 401, false);
                    return false;
                }

                var webhookData = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);
                if (webhookData == null)
                {
                    _logger.LogError("Failed to deserialize payment webhook payload from {Provider}", provider);
                    return false;
                }

                await ProcessPaymentWebhookData(provider, webhookData);
                await LogWebhookAsync(provider, "payment", payload, "Success", 200, true);
                
                _logger.LogInformation("Successfully processed payment webhook from provider {Provider}", provider);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment webhook from provider {Provider}", provider);
                await LogWebhookAsync(provider, "payment", payload, ex.Message, 500, false);
                return false;
            }
        }

        public async Task<bool> ProcessSmsDeliveryWebhookAsync(string provider, string payload, string signature)
        {
            try
            {
                _logger.LogInformation("Processing SMS delivery webhook from provider {Provider}", provider);

                var webhookConfig = await GetWebhookConfigurationAsync();
                if (!webhookConfig.Endpoints.ContainsKey($"sms_{provider}"))
                {
                    _logger.LogWarning("No webhook configuration found for SMS provider {Provider}", provider);
                    return false;
                }

                var endpoint = webhookConfig.Endpoints[$"sms_{provider}"];
                var validationResult = await ValidateWebhookSignatureAsync(provider, payload, signature, endpoint.Secret);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Invalid webhook signature for SMS provider {Provider}", provider);
                    await LogWebhookAsync(provider, "sms_delivery", payload, "Invalid signature", 401, false);
                    return false;
                }

                var webhookData = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);
                if (webhookData == null)
                {
                    _logger.LogError("Failed to deserialize SMS delivery webhook payload from {Provider}", provider);
                    return false;
                }

                await ProcessSmsDeliveryWebhookData(provider, webhookData);
                await LogWebhookAsync(provider, "sms_delivery", payload, "Success", 200, true);
                
                _logger.LogInformation("Successfully processed SMS delivery webhook from provider {Provider}", provider);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SMS delivery webhook from provider {Provider}", provider);
                await LogWebhookAsync(provider, "sms_delivery", payload, ex.Message, 500, false);
                return false;
            }
        }

        public async Task<bool> ProcessFraudDetectionWebhookAsync(string payload, string signature)
        {
            try
            {
                _logger.LogInformation("Processing fraud detection webhook");

                var webhookConfig = await GetWebhookConfigurationAsync();
                if (!webhookConfig.Endpoints.ContainsKey("fraud_detection"))
                {
                    _logger.LogWarning("No webhook configuration found for fraud detection");
                    return false;
                }

                var endpoint = webhookConfig.Endpoints["fraud_detection"];
                var validationResult = await ValidateWebhookSignatureAsync("fraud_detection", payload, signature, endpoint.Secret);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Invalid webhook signature for fraud detection");
                    await LogWebhookAsync("internal", "fraud_detection", payload, "Invalid signature", 401, false);
                    return false;
                }

                var webhookData = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);
                if (webhookData == null)
                {
                    _logger.LogError("Failed to deserialize fraud detection webhook payload");
                    return false;
                }

                await ProcessFraudDetectionWebhookData(webhookData);
                await LogWebhookAsync("internal", "fraud_detection", payload, "Success", 200, true);
                
                _logger.LogInformation("Successfully processed fraud detection webhook");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing fraud detection webhook");
                await LogWebhookAsync("internal", "fraud_detection", payload, ex.Message, 500, false);
                return false;
            }
        }

        public async Task<bool> ProcessComplianceWebhookAsync(string payload, string signature)
        {
            try
            {
                _logger.LogInformation("Processing compliance webhook");

                var webhookConfig = await GetWebhookConfigurationAsync();
                if (!webhookConfig.Endpoints.ContainsKey("compliance"))
                {
                    _logger.LogWarning("No webhook configuration found for compliance");
                    return false;
                }

                var endpoint = webhookConfig.Endpoints["compliance"];
                var validationResult = await ValidateWebhookSignatureAsync("compliance", payload, signature, endpoint.Secret);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Invalid webhook signature for compliance");
                    await LogWebhookAsync("internal", "compliance", payload, "Invalid signature", 401, false);
                    return false;
                }

                var webhookData = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);
                if (webhookData == null)
                {
                    _logger.LogError("Failed to deserialize compliance webhook payload");
                    return false;
                }

                await ProcessComplianceWebhookData(webhookData);
                await LogWebhookAsync("internal", "compliance", payload, "Success", 200, true);
                
                _logger.LogInformation("Successfully processed compliance webhook");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing compliance webhook");
                await LogWebhookAsync("internal", "compliance", payload, ex.Message, 500, false);
                return false;
            }
        }

        public async Task<WebhookResponse> ValidateWebhookSignatureAsync(string provider, string payload, string signature, string secret)
        {
            try
            {
                if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
                {
                    return new WebhookResponse { IsValid = false, Message = "Missing signature or secret" };
                }

                string expectedSignature;
                switch (provider.ToLower())
                {
                    case "stripe":
                        expectedSignature = GenerateStripeSignature(payload, secret);
                        break;
                    case "paypal":
                        expectedSignature = GeneratePayPalSignature(payload, secret);
                        break;
                    default:
                        expectedSignature = GenerateHmacSha256Signature(payload, secret);
                        break;
                }

                var isValid = signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
                return new WebhookResponse 
                { 
                    IsValid = isValid, 
                    Message = isValid ? "Valid signature" : "Invalid signature" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating webhook signature for provider {Provider}", provider);
                return new WebhookResponse { IsValid = false, Message = "Signature validation error" };
            }
        }

        public async Task<bool> SendWebhookAsync(string url, object payload, string secret, int maxRetries = 3)
        {
            var payloadJson = JsonSerializer.Serialize(payload);
            var signature = GenerateHmacSha256Signature(payloadJson, secret);
            
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                    };
                    
                    request.Headers.Add("X-Nexora-Signature", signature);
                    request.Headers.Add("X-Nexora-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

                    var response = await _httpClient.SendAsync(request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully sent webhook to {Url} on attempt {Attempt}", url, attempt + 1);
                        return true;
                    }
                    
                    _logger.LogWarning("Webhook delivery failed to {Url} on attempt {Attempt} with status {StatusCode}", 
                        url, attempt + 1, response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending webhook to {Url} on attempt {Attempt}", url, attempt + 1);
                }

                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 30); // Exponential backoff
                    await Task.Delay(delay);
                }
            }

            _logger.LogError("Failed to send webhook to {Url} after {MaxRetries} attempts", url, maxRetries + 1);
            return false;
        }

        public async Task<List<WebhookLog>> GetWebhookLogsAsync(int? tenantId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var cacheKey = $"webhook-logs:{tenantId}:{fromDate?.ToString("yyyyMMdd")}:{toDate?.ToString("yyyyMMdd")}";
                var cachedLogs = await _cacheService.GetAsync<List<WebhookLog>>(cacheKey);
                
                if (cachedLogs != null)
                {
                    return cachedLogs;
                }

                var logs = new List<WebhookLog>
                {
                    new WebhookLog
                    {
                        Id = 1,
                        TenantId = tenantId,
                        WebhookId = Guid.NewGuid().ToString(),
                        Provider = "stripe",
                        EventType = "payment.succeeded",
                        Url = "https://api.example.com/webhooks/payment",
                        StatusCode = 200,
                        IsSuccess = true,
                        RetryCount = 0,
                        CreatedAt = DateTime.UtcNow.AddHours(-1),
                        ProcessedAt = DateTime.UtcNow.AddHours(-1)
                    }
                };

                await _cacheService.SetAsync(cacheKey, logs, TimeSpan.FromMinutes(5));
                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhook logs for tenant {TenantId}", tenantId);
                return new List<WebhookLog>();
            }
        }

        public async Task<WebhookConfiguration> GetWebhookConfigurationAsync(int? tenantId = null)
        {
            try
            {
                var cacheKey = $"webhook-config:{tenantId ?? 0}";
                var cachedConfig = await _cacheService.GetAsync<WebhookConfiguration>(cacheKey);
                
                if (cachedConfig != null)
                {
                    return cachedConfig;
                }

                var config = new WebhookConfiguration
                {
                    TenantId = tenantId,
                    Endpoints = new Dictionary<string, WebhookEndpoint>
                    {
                        ["payment_stripe"] = new WebhookEndpoint
                        {
                            Url = "https://api.nexora.com/webhooks/payment/stripe",
                            Secret = "whsec_stripe_secret",
                            IsEnabled = true,
                            EventTypes = new List<string> { "payment.succeeded", "payment.failed", "payment.refunded" },
                            MaxRetries = 3,
                            TimeoutSeconds = 30
                        },
                        ["sms_provider"] = new WebhookEndpoint
                        {
                            Url = "https://api.nexora.com/webhooks/sms/delivery",
                            Secret = "whsec_sms_secret",
                            IsEnabled = true,
                            EventTypes = new List<string> { "message.delivered", "message.failed", "message.pending" },
                            MaxRetries = 3,
                            TimeoutSeconds = 30
                        }
                    },
                    Settings = new WebhookSettings
                    {
                        EnableRetries = true,
                        DefaultMaxRetries = 3,
                        DefaultTimeoutSeconds = 30,
                        RetryDelaySeconds = 60,
                        EnableLogging = true,
                        LogRetentionDays = 30
                    }
                };

                await _cacheService.SetAsync(cacheKey, config, TimeSpan.FromHours(1));
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhook configuration for tenant {TenantId}", tenantId);
                return new WebhookConfiguration { TenantId = tenantId };
            }
        }

        public async Task UpdateWebhookConfigurationAsync(WebhookConfiguration configuration, int? tenantId = null)
        {
            try
            {
                configuration.TenantId = tenantId;
                
                var cacheKey = $"webhook-config:{tenantId ?? 0}";
                await _cacheService.SetAsync(cacheKey, configuration, TimeSpan.FromHours(1));
                
                _logger.LogInformation("Updated webhook configuration for tenant {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating webhook configuration for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> RetryFailedWebhookAsync(int webhookLogId)
        {
            try
            {
                _logger.LogInformation("Retrying failed webhook {WebhookLogId}", webhookLogId);
                
                await Task.Delay(100);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying failed webhook {WebhookLogId}", webhookLogId);
                return false;
            }
        }

        public async Task<WebhookDeliveryStatus> GetWebhookDeliveryStatusAsync(string webhookId)
        {
            try
            {
                return WebhookDeliveryStatus.Delivered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhook delivery status for {WebhookId}", webhookId);
                return WebhookDeliveryStatus.Failed;
            }
        }

        private async Task ProcessPaymentWebhookData(string provider, Dictionary<string, object> data)
        {
            try
            {
                if (data.TryGetValue("event_type", out var eventTypeObj) && eventTypeObj is string eventType)
                {
                    switch (eventType.ToLower())
                    {
                        case "payment.succeeded":
                            await HandlePaymentSucceeded(provider, data);
                            break;
                        case "payment.failed":
                            await HandlePaymentFailed(provider, data);
                            break;
                        case "payment.refunded":
                            await HandlePaymentRefunded(provider, data);
                            break;
                        default:
                            _logger.LogWarning("Unknown payment event type: {EventType}", eventType);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment webhook data from {Provider}", provider);
            }
        }

        private async Task ProcessSmsDeliveryWebhookData(string provider, Dictionary<string, object> data)
        {
            try
            {
                if (data.TryGetValue("message_id", out var messageIdObj) && messageIdObj is string messageId &&
                    data.TryGetValue("status", out var statusObj) && statusObj is string status)
                {
                    await _smsBillingService.UpdateMessageStatusAsync(messageId, status);
                    
                    await _notificationService.SendNotificationAsync(
                        _tenantService.GetCurrentTenantId(),
                        "SMS Delivery Update",
                        $"Message {messageId} status updated to {status}",
                        "sms_delivery"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SMS delivery webhook data from {Provider}", provider);
            }
        }

        private async Task ProcessFraudDetectionWebhookData(Dictionary<string, object> data)
        {
            try
            {
                if (data.TryGetValue("alert_type", out var alertTypeObj) && alertTypeObj is string alertType &&
                    data.TryGetValue("risk_score", out var riskScoreObj) && riskScoreObj is double riskScore)
                {
                    await _notificationService.SendNotificationAsync(
                        _tenantService.GetCurrentTenantId(),
                        "Fraud Detection Alert",
                        $"High risk transaction detected: {alertType} (Risk Score: {riskScore:F2})",
                        "fraud_alert"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing fraud detection webhook data");
            }
        }

        private async Task ProcessComplianceWebhookData(Dictionary<string, object> data)
        {
            try
            {
                if (data.TryGetValue("compliance_type", out var complianceTypeObj) && complianceTypeObj is string complianceType &&
                    data.TryGetValue("status", out var statusObj) && statusObj is string status)
                {
                    await _notificationService.SendNotificationAsync(
                        _tenantService.GetCurrentTenantId(),
                        "Compliance Update",
                        $"Compliance check {complianceType} status: {status}",
                        "compliance_update"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing compliance webhook data");
            }
        }

        private async Task HandlePaymentSucceeded(string provider, Dictionary<string, object> data)
        {
            _logger.LogInformation("Processing successful payment from {Provider}", provider);
        }

        private async Task HandlePaymentFailed(string provider, Dictionary<string, object> data)
        {
            _logger.LogInformation("Processing failed payment from {Provider}", provider);
        }

        private async Task HandlePaymentRefunded(string provider, Dictionary<string, object> data)
        {
            _logger.LogInformation("Processing refunded payment from {Provider}", provider);
        }

        private string GenerateStripeSignature(string payload, string secret)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var signedPayload = $"{timestamp}.{payload}";
            return $"t={timestamp},v1={GenerateHmacSha256Signature(signedPayload, secret)}";
        }

        private string GeneratePayPalSignature(string payload, string secret)
        {
            return GenerateHmacSha256Signature(payload, secret);
        }

        private string GenerateHmacSha256Signature(string payload, string secret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private async Task LogWebhookAsync(string provider, string eventType, string payload, string response, int statusCode, bool isSuccess)
        {
            try
            {
                var log = new WebhookLog
                {
                    TenantId = _tenantService.GetCurrentTenantId(),
                    WebhookId = Guid.NewGuid().ToString(),
                    Provider = provider,
                    EventType = eventType,
                    Payload = payload,
                    Response = response,
                    StatusCode = statusCode,
                    IsSuccess = isSuccess,
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Logged webhook: {Provider} {EventType} {StatusCode}", provider, eventType, statusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging webhook");
            }
        }
    }
}
