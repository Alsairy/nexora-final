using System;
using System.Collections.Generic;

namespace Nexora.Core.DTOs
{
    public class SendSmsRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string? SenderId { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public string Priority { get; set; } = "Normal";
        public int TenantId { get; set; }
        public bool IsUnicode { get; set; } = false;
    }
}
