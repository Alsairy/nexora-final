using System;
using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.Entities
{
    public class NotificationLog : IEntity, ITenantEntity, ISoftDelete, IAuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TenantId { get; set; }
        public string UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationChannel Channel { get; set; }
        public NotificationStatus Status { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string ExternalId { get; set; }
        public string ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;
        public DateTime? NextRetryAt { get; set; }
        public string Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual User User { get; set; }
    }

    public enum NotificationType
    {
        PaymentSuccess = 1,
        PaymentFailure = 2,
        SmsDelivered = 3,
        SmsFailed = 4,
        BillingThreshold = 5,
        SubscriptionRenewal = 6,
        ComplianceAlert = 7,
        SystemAlert = 8,
        SecurityAlert = 9
    }

    public enum NotificationChannel
    {
        InApp = 1,
        Email = 2,
        Sms = 3,
        Push = 4,
        Webhook = 5
    }

    public enum NotificationStatus
    {
        Pending = 1,
        Sent = 2,
        Delivered = 3,
        Read = 4,
        Failed = 5,
        Cancelled = 6
    }
}
