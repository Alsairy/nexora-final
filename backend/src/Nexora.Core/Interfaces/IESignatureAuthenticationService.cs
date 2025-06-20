using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;

namespace Nexora.Core.Interfaces
{
    public interface IESignatureAuthenticationService
    {
        Task<AuthenticationResult> InitiateOtpAuthenticationAsync(string phoneNumber, int documentId, int signerId, int tenantId);
        Task<AuthenticationResult> VerifyOtpAuthenticationAsync(string phoneNumber, string code, int documentId, int signerId, int tenantId);
        Task<AuthenticationResult> ResendOtpAsync(string phoneNumber, int documentId, int signerId, int tenantId);
        
        Task<AuthenticationResult> InitiateNafathAuthenticationAsync(string nationalId, int documentId, int signerId, int tenantId);
        Task<AuthenticationResult> VerifyNafathAuthenticationAsync(string transactionId, string code, int documentId, int signerId, int tenantId);
        Task<AuthenticationResult> CheckNafathStatusAsync(string transactionId, int documentId, int signerId, int tenantId);
        
        Task<AuthenticationResult> InitiateCertificateAuthenticationAsync(string certificateData, int documentId, int signerId, int tenantId);
        Task<AuthenticationResult> VerifyCertificateAuthenticationAsync(string certificateData, string signature, int documentId, int signerId, int tenantId);
        Task<bool> ValidateDigitalCertificateAsync(string certificateData, int tenantId);
        
        Task<AuthenticationResult> InitiateBiometricAuthenticationAsync(string biometricType, int documentId, int signerId, int tenantId);
        Task<AuthenticationResult> VerifyBiometricAuthenticationAsync(string biometricData, string biometricType, int documentId, int signerId, int tenantId);
        
        Task<AuthenticationResult> InitiatePasswordAuthenticationAsync(string email, int documentId, int signerId, int tenantId);
        Task<AuthenticationResult> VerifyPasswordAuthenticationAsync(string email, string password, int documentId, int signerId, int tenantId);
        
        Task<bool> IsAuthenticationValidAsync(int authenticationId, int tenantId);
        Task<bool> IsAuthenticationExpiredAsync(int authenticationId, int tenantId);
        Task<ESignatureAuthentication> GetAuthenticationAsync(int authenticationId, int tenantId);
        Task<List<ESignatureAuthentication>> GetDocumentAuthenticationsAsync(int documentId, int tenantId);
        Task<List<ESignatureAuthentication>> GetSignerAuthenticationsAsync(int signerId, int tenantId);
        
        Task<bool> ValidateAuthenticationMethodAsync(string authenticationMethod, int tenantId);
        Task<List<string>> GetSupportedAuthenticationMethodsAsync(int tenantId);
        Task<Dictionary<string, object>> GetAuthenticationConfigurationAsync(string authenticationMethod, int tenantId);
        
        Task<AuthenticationResult> RefreshAuthenticationAsync(int authenticationId, int tenantId);
        Task<bool> RevokeAuthenticationAsync(int authenticationId, int tenantId, int userId);
        
        Task<List<ESignatureAuthentication>> GetFailedAuthenticationAttemptsAsync(int documentId, int signerId, int tenantId);
        Task<bool> IsSignerBlockedAsync(int signerId, int tenantId);
        Task<DateTime?> GetSignerBlockExpiryAsync(int signerId, int tenantId);
        
        Task<Dictionary<string, object>> GetAuthenticationMetricsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<AuthenticationSecurityEvent>> GetSecurityEventsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
        
        Task ProcessExpiredAuthenticationsAsync(int tenantId);
        Task CleanupOldAuthenticationDataAsync(int tenantId, TimeSpan retentionPeriod);
        Task<bool> ValidateSignerAuthenticationAsync(int signerId, CancellationToken cancellationToken = default);
    }

    public class AuthenticationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string TransactionId { get; set; }
        public string Code { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? AuthenticationId { get; set; }
        public string Provider { get; set; }
        public string ProviderResponse { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
        public List<string> Errors { get; set; } = new List<string>();
        public string RiskLevel { get; set; }
        public bool RequiresAdditionalVerification { get; set; }
        public string NextStep { get; set; }
    }

    public class AuthenticationSecurityEvent
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RiskLevel { get; set; }
        public int? DocumentId { get; set; }
        public int? SignerId { get; set; }
        public string AuthenticationMethod { get; set; }
        public bool IsBlocked { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
