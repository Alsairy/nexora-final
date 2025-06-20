using System;
using System.Collections.Generic;

namespace Nexora.Core.DTOs
{
    public class NotificationRequest
    {
        public string Type { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public List<string> Recipients { get; set; } = new List<string>();
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public DateTime? ScheduledAt { get; set; }
        public string Priority { get; set; } = "Normal";
        public string Channel { get; set; } = "Email";
        public int? TenantId { get; set; }
        public int? UserId { get; set; }
    }
}
