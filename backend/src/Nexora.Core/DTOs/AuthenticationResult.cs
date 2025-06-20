using System;

namespace Nexora.Core.DTOs
{
    public class AuthenticationResult
    {
        public bool IsSuccess { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public string TransactionId { get; set; }
        public DateTime? AuthenticatedAt { get; set; }
        public string AuthenticationMethod { get; set; }
        public string Provider { get; set; }
        public string ProviderResponse { get; set; }
        public string SessionId { get; set; }
        public string Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string UserIdentifier { get; set; }
        public string RiskScore { get; set; }
        public string RiskLevel { get; set; }
        public bool RequiresAdditionalVerification { get; set; }
        public string AdditionalVerificationReason { get; set; }
        public string ComplianceData { get; set; }
        public string AuditTrail { get; set; }
        public string Metadata { get; set; }
    }
}
