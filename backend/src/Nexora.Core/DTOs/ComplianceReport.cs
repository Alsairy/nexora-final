using System;
using System.Collections.Generic;

namespace Nexora.Core.DTOs
{
    public class ComplianceReport
    {
        public int TenantId { get; set; }
        public DateTime ReportPeriodStart { get; set; }
        public DateTime ReportPeriodEnd { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public int TotalDocuments { get; set; }
        public int CompliantDocuments { get; set; }
        public double ComplianceRate { get; set; }
        public List<ComplianceViolation> Violations { get; set; } = new List<ComplianceViolation>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }
}
