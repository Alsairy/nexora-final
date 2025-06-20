using System;
using System.Collections.Generic;

namespace Nexora.Core.DTOs
{
    public class SendEmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string ToEmail { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string TextBody { get; set; } = string.Empty;
        public string EmailType { get; set; } = string.Empty;
        public string? From { get; set; }
        public List<string> Cc { get; set; } = new List<string>();
        public List<string> Bcc { get; set; } = new List<string>();
        public Dictionary<string, byte[]> Attachments { get; set; } = new Dictionary<string, byte[]>();
        public bool IsHtml { get; set; } = true;
        public string Priority { get; set; } = "Normal";
        public int? TenantId { get; set; }
    }
}
