using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.DTOs
{
    public class SendSmsRequest
    {
        [Required]
        [MaxLength(20)]
        public string ToNumber { get; set; }

        [Required]
        [MaxLength(1600)]
        public string Message { get; set; }

        [MaxLength(20)]
        public string SenderId { get; set; }

        [Required]
        [MaxLength(50)]
        public string MessageType { get; set; } = "Transactional";

        public int? TemplateId { get; set; }

        public DateTime? ScheduledAt { get; set; }

        [MaxLength(100)]
        public string CampaignId { get; set; }

        public int Priority { get; set; } = 1;

        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

        [MaxLength(500)]
        public string Metadata { get; set; }
    }

    public class SendBulkSmsRequest
    {
        [Required]
        public List<BulkSmsRecipient> Recipients { get; set; } = new List<BulkSmsRecipient>();

        [Required]
        [MaxLength(1600)]
        public string Message { get; set; }

        [MaxLength(20)]
        public string SenderId { get; set; }

        [Required]
        [MaxLength(50)]
        public string MessageType { get; set; } = "Transactional";

        public int? TemplateId { get; set; }

        public DateTime? ScheduledAt { get; set; }

        [MaxLength(100)]
        public string CampaignId { get; set; }

        public int Priority { get; set; } = 1;

        [MaxLength(500)]
        public string Metadata { get; set; }
    }

    public class BulkSmsRecipient
    {
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    }

    public class SmsResponse
    {
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string Status { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; } = "SAR";
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BulkSmsResponse
    {
        public bool Success { get; set; }
        public int TotalMessages { get; set; }
        public int SuccessfulMessages { get; set; }
        public int FailedMessages { get; set; }
        public decimal TotalCost { get; set; }
        public string Currency { get; set; } = "SAR";
        public List<SmsResponse> Messages { get; set; } = new List<SmsResponse>();
        public string BatchId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SmsMessageDto
    {
        public int Id { get; set; }
        public string MessageId { get; set; }
        public string FromNumber { get; set; }
        public string ToNumber { get; set; }
        public string MessageContent { get; set; }
        public string Status { get; set; }
        public string SenderId { get; set; }
        public string MessageType { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; }
        public string ProviderId { get; set; }
        public string ProviderMessageId { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public int RetryCount { get; set; }
        public bool IsCompliant { get; set; }
        public string ComplianceNotes { get; set; }
        public string CampaignId { get; set; }
        public string BatchId { get; set; }
        public int Priority { get; set; }
        public string Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SmsDeliveryStatusRequest
    {
        [Required]
        public string MessageId { get; set; }
    }

    public class SmsDeliveryStatusResponse
    {
        public string MessageId { get; set; }
        public string Status { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; }
        public string ProviderId { get; set; }
        public string ProviderMessageId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SmsListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string Status { get; set; }
        public string MessageType { get; set; }
        public string SenderId { get; set; }
        public string CampaignId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; } = "CreatedAt";
        public string SortOrder { get; set; } = "DESC";
    }

    public class SmsListResponse
    {
        public List<SmsMessageDto> Messages { get; set; } = new List<SmsMessageDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class SmsProviderDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string Country { get; set; }
        public string SupportedNetworks { get; set; }
        public decimal CostPerSms { get; set; }
        public string Currency { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public decimal? SuccessRate { get; set; }
        public decimal? AverageDeliveryTime { get; set; }
        public string HealthStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateSmsProviderRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        [MaxLength(200)]
        public string ApiEndpoint { get; set; }

        [MaxLength(100)]
        public string ApiKey { get; set; }

        [MaxLength(100)]
        public string ApiSecret { get; set; }

        [MaxLength(100)]
        public string Username { get; set; }

        [MaxLength(100)]
        public string Password { get; set; }

        [MaxLength(50)]
        public string AuthType { get; set; } = "ApiKey";

        [Required]
        [MaxLength(50)]
        public string Country { get; set; } = "SA";

        [MaxLength(100)]
        public string SupportedNetworks { get; set; }

        public decimal CostPerSms { get; set; }

        public int Priority { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        public int MaxConcurrentMessages { get; set; } = 100;

        public int RateLimitPerSecond { get; set; } = 10;

        public bool IsKsaCompliant { get; set; } = false;

        public bool SupportsCitcIntegration { get; set; } = false;

        [MaxLength(100)]
        public string CitcProviderId { get; set; }

        [MaxLength(500)]
        public string ConfigurationJson { get; set; }
    }

    public class UpdateSmsProviderRequest
    {
        [MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(200)]
        public string ApiEndpoint { get; set; }

        [MaxLength(100)]
        public string ApiKey { get; set; }

        [MaxLength(100)]
        public string ApiSecret { get; set; }

        [MaxLength(100)]
        public string Username { get; set; }

        [MaxLength(100)]
        public string Password { get; set; }

        [MaxLength(100)]
        public string SupportedNetworks { get; set; }

        public decimal? CostPerSms { get; set; }

        public int? Priority { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsDefault { get; set; }

        public int? MaxConcurrentMessages { get; set; }

        public int? RateLimitPerSecond { get; set; }

        public bool? IsKsaCompliant { get; set; }

        public bool? SupportsCitcIntegration { get; set; }

        [MaxLength(100)]
        public string CitcProviderId { get; set; }

        [MaxLength(500)]
        public string ConfigurationJson { get; set; }
    }
}
