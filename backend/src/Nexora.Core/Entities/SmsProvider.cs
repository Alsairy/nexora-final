using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexora.Core.Entities
{
    public class SmsProvider : IEntity, IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

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

        [Column(TypeName = "decimal(10,6)")]
        public decimal CostPerSms { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "SAR";

        public int Priority { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        public bool SupportsDeliveryReceipts { get; set; } = true;

        public bool SupportsUnicode { get; set; } = true;

        public bool SupportsLongMessages { get; set; } = true;

        public int MaxMessageLength { get; set; } = 160;

        public int MaxConcurrentMessages { get; set; } = 100;

        public int RateLimitPerSecond { get; set; } = 10;

        public int RateLimitPerMinute { get; set; } = 600;

        public int RateLimitPerHour { get; set; } = 36000;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? SuccessRate { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal? AverageDeliveryTime { get; set; }

        public DateTime? LastHealthCheck { get; set; }

        [MaxLength(50)]
        public string HealthStatus { get; set; } = "Unknown";

        [MaxLength(500)]
        public string HealthStatusMessage { get; set; }

        public int FailureCount { get; set; } = 0;

        public DateTime? LastFailureAt { get; set; }

        [MaxLength(500)]
        public string LastFailureReason { get; set; }

        public bool IsKsaCompliant { get; set; } = false;

        public bool SupportsCitcIntegration { get; set; } = false;

        [MaxLength(100)]
        public string CitcProviderId { get; set; }

        [MaxLength(500)]
        public string ConfigurationJson { get; set; }

        [MaxLength(500)]
        public string WebhookUrl { get; set; }

        [MaxLength(100)]
        public string WebhookSecret { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        [MaxLength(100)]
        public string UpdatedBy { get; set; }

        public virtual ICollection<SmsMessage> Messages { get; set; } = new List<SmsMessage>();
        public virtual ICollection<SenderId> SenderIds { get; set; } = new List<SenderId>();
    }
}
