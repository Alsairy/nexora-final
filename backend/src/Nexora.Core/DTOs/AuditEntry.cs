using System;
using System.Collections.Generic;

namespace Nexora.Core.DTOs
{
    public class AuditEntry
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string Action { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public Dictionary<string, object?> Changes { get; set; } = new Dictionary<string, object?>();
        public Dictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();
        public string Description { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; }
        public string Source { get; set; }
        public string CorrelationId { get; set; }
        public string SessionId { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
    }
}
