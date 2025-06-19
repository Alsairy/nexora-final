using Nexora.Core.DTOs;

namespace Nexora.Core.Interfaces
{
    public interface IWebhookService
    {
        Task<bool> ProcessPaymentWebhookAsync(string provider, string payload, string signature);
        Task<bool> ProcessSmsDeliveryWebhookAsync(string provider, string payload, string signature);
        Task<bool> ProcessFraudDetectionWebhookAsync(string payload, string signature);
        Task<bool> ProcessComplianceWebhookAsync(string payload, string signature);
        
        Task<WebhookResponse> ValidateWebhookSignatureAsync(string provider, string payload, string signature, string secret);
        Task<bool> SendWebhookAsync(string url, object payload, string secret, int maxRetries = 3);
        
        Task<List<WebhookLog>> GetWebhookLogsAsync(int? tenantId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<WebhookConfiguration> GetWebhookConfigurationAsync(int? tenantId = null);
        Task UpdateWebhookConfigurationAsync(WebhookConfiguration configuration, int? tenantId = null);
        
        Task<bool> RetryFailedWebhookAsync(int webhookLogId);
        Task<WebhookDeliveryStatus> GetWebhookDeliveryStatusAsync(string webhookId);
    }

    public class WebhookResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public class WebhookLog
    {
        public int Id { get; set; }
        public int? TenantId { get; set; }
        public string WebhookId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class WebhookConfiguration
    {
        public int? TenantId { get; set; }
        public Dictionary<string, WebhookEndpoint> Endpoints { get; set; } = new();
        public WebhookSettings Settings { get; set; } = new();
    }

    public class WebhookEndpoint
    {
        public string Url { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public List<string> EventTypes { get; set; } = new();
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class WebhookSettings
    {
        public bool EnableRetries { get; set; } = true;
        public int DefaultMaxRetries { get; set; } = 3;
        public int DefaultTimeoutSeconds { get; set; } = 30;
        public int RetryDelaySeconds { get; set; } = 60;
        public bool EnableLogging { get; set; } = true;
        public int LogRetentionDays { get; set; } = 30;
    }

    public enum WebhookDeliveryStatus
    {
        Pending,
        Delivered,
        Failed,
        Retrying,
        Expired
    }
}
