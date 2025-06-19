using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexora.Core.Interfaces
{
    public interface INotificationService
    {
        Task SendPaymentStatusNotificationAsync(string tenantId, string userId, PaymentNotification notification);
        Task SendSmsDeliveryNotificationAsync(string tenantId, string userId, SmsDeliveryNotification notification);
        Task SendPaymentFailureAlertAsync(string tenantId, string userId, PaymentFailureAlert alert);
        Task SendBillingThresholdNotificationAsync(string tenantId, string userId, BillingThresholdNotification notification);
        Task SendMultiChannelNotificationAsync(string tenantId, string userId, MultiChannelNotification notification);
        Task SubscribeToNotificationsAsync(string connectionId, string tenantId, string userId);
        Task UnsubscribeFromNotificationsAsync(string connectionId);
        Task<List<NotificationHistory>> GetNotificationHistoryAsync(string tenantId, string userId, int pageSize = 50, int pageNumber = 1);
    }

    public class PaymentNotification
    {
        public string PaymentId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "SAR";
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SmsDeliveryNotification
    {
        public string MessageId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public SmsDeliveryStatus Status { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; } = "SAR";
        public string Provider { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class PaymentFailureAlert
    {
        public string PaymentId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public PaymentFailureReason Reason { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "SAR";
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool RequiresAction { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BillingThresholdNotification
    {
        public string ThresholdId { get; set; } = string.Empty;
        public BillingThresholdType Type { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal ThresholdAmount { get; set; }
        public decimal PercentageUsed { get; set; }
        public string Currency { get; set; } = "SAR";
        public string Service { get; set; } = string.Empty; // SMS, WhatsApp, Voice, etc.
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public BillingThresholdSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ActionUrl { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class MultiChannelNotification
    {
        public string NotificationId { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationPriority Priority { get; set; }
        public List<NotificationChannel> Channels { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class NotificationHistory
    {
        public string Id { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Refunded,
        PartiallyRefunded
    }

    public enum SmsDeliveryStatus
    {
        Queued,
        Sent,
        Delivered,
        Failed,
        Expired,
        Rejected
    }

    public enum PaymentFailureReason
    {
        InsufficientFunds,
        InvalidCard,
        ExpiredCard,
        CardDeclined,
        NetworkError,
        ProcessingError,
        FraudDetected,
        LimitExceeded,
        Unknown
    }

    public enum BillingThresholdType
    {
        Usage,
        Cost,
        MessageCount,
        ApiCalls
    }

    public enum BillingThresholdSeverity
    {
        Info,
        Warning,
        Critical,
        Emergency
    }

    public enum NotificationType
    {
        PaymentStatus,
        SmsDelivery,
        PaymentFailure,
        BillingThreshold,
        SystemAlert,
        SecurityAlert,
        MaintenanceNotice,
        FeatureUpdate
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public enum NotificationChannel
    {
        WebSocket,
        Email,
        SMS,
        WhatsApp,
        Push,
        InApp
    }

    public enum NotificationStatus
    {
        Pending,
        Sent,
        Delivered,
        Read,
        Failed,
        Expired
    }
}
