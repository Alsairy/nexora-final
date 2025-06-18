using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexora.Core.Interfaces
{
    public interface IKsaComplianceService
    {
        Task<ComplianceCheckResult> CheckComplianceAsync(string phoneNumber, string message, string messageType, string senderId, int tenantId);
        Task<bool> IsDndNumberAsync(string phoneNumber);
        Task<bool> IsTimeWindowAllowedAsync(string messageType, DateTime? scheduledTime = null);
        Task<bool> IsContentCompliantAsync(string message, string messageType);
        Task<bool> IsSenderIdApprovedAsync(string senderId, int tenantId);
        Task<List<string>> GetProhibitedKeywordsAsync();
        Task<bool> ValidateMessageContentAsync(string message, string messageType);
        Task<bool> CheckUrlComplianceAsync(string message);
        Task<ComplianceReport> GenerateComplianceReportAsync(int tenantId, DateTime fromDate, DateTime toDate);
        Task<bool> RegisterSenderIdAsync(string senderId, int tenantId, string businessName, string businessType);
        Task<SenderIdStatus> GetSenderIdStatusAsync(string senderId, int tenantId);
        Task<bool> UpdateDndListAsync(List<string> phoneNumbers, bool isDnd);
        Task<List<string>> GetDndNumbersAsync(int tenantId);
        Task<bool> ValidatePhoneNumberFormatAsync(string phoneNumber);
        Task<string> NormalizePhoneNumberAsync(string phoneNumber);
        Task<bool> IsBusinessHoursAsync(DateTime? dateTime = null);
        Task<TimeWindow> GetAllowedTimeWindowAsync(string messageType);
        Task<bool> CheckRateLimitComplianceAsync(string senderId, int tenantId);
        Task<bool> LogComplianceViolationAsync(string phoneNumber, string violation, string messageType, int tenantId);
        Task<List<ComplianceViolation>> GetComplianceViolationsAsync(int tenantId, DateTime fromDate, DateTime toDate);
        Task<bool> IsOptedOutAsync(string phoneNumber, int tenantId);
        Task<bool> ProcessOptOutRequestAsync(string phoneNumber, int tenantId);
        Task<bool> ProcessOptInRequestAsync(string phoneNumber, int tenantId);
        Task<CitcIntegrationStatus> GetCitcIntegrationStatusAsync(int tenantId);
        Task<bool> SyncWithCitcAsync(int tenantId);
        Task<bool> ValidateMessageTemplateAsync(string templateContent, string messageType);
        Task<List<string>> GetComplianceRecommendationsAsync(string message, string messageType);
    }

    public class ComplianceCheckResult
    {
        public bool IsCompliant { get; set; }
        public List<string> Violations { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public bool DndChecked { get; set; }
        public bool TimeWindowChecked { get; set; }
        public bool ContentFiltered { get; set; }
        public bool SenderIdValidated { get; set; }
        public string ComplianceScore { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    public class ComplianceReport
    {
        public int TenantId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalMessages { get; set; }
        public int CompliantMessages { get; set; }
        public int ViolationCount { get; set; }
        public List<ComplianceViolation> Violations { get; set; } = new List<ComplianceViolation>();
        public Dictionary<string, int> ViolationsByType { get; set; } = new Dictionary<string, int>();
        public decimal ComplianceRate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class ComplianceViolation
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string PhoneNumber { get; set; }
        public string ViolationType { get; set; }
        public string Description { get; set; }
        public string MessageType { get; set; }
        public string SenderId { get; set; }
        public DateTime OccurredAt { get; set; }
        public string Severity { get; set; }
        public bool IsResolved { get; set; }
        public string Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class SenderIdStatus
    {
        public string SenderId { get; set; }
        public int TenantId { get; set; }
        public string Status { get; set; } // Pending, Approved, Rejected, Suspended
        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string RejectionReason { get; set; }
        public string BusinessName { get; set; }
        public string BusinessType { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class TimeWindow
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public List<DayOfWeek> AllowedDays { get; set; } = new List<DayOfWeek>();
        public string TimeZone { get; set; } = "Asia/Riyadh";
        public bool IsBusinessHoursOnly { get; set; }
    }

    public class CitcIntegrationStatus
    {
        public int TenantId { get; set; }
        public bool IsIntegrated { get; set; }
        public string IntegrationId { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
