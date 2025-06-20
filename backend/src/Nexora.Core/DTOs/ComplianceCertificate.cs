using System;
using System.Collections.Generic;

namespace Nexora.Core.DTOs
{
    public class ComplianceCertificate
    {
        public string Id { get; set; } = string.Empty;
        public int DocumentId { get; set; }
        public string CertificateType { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string IssuedBy { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsValid { get; set; }
        public List<string> ComplianceStandards { get; set; } = new List<string>();
        public Dictionary<string, object> CertificateData { get; set; } = new Dictionary<string, object>();
        public int TenantId { get; set; }
    }
}
