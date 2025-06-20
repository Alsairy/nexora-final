using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Core.Data;
using Nexora.Core.Models;
using AuthenticationResult = Nexora.Core.Interfaces.AuthenticationResult;
using AuditEntry = Nexora.Core.DTOs.AuditEntry;

namespace Nexora.Infrastructure.Services
{
    public class ESignatureAuthenticationService : IESignatureAuthenticationService
    {
        private readonly NexoraDbContext _context;
        private readonly ILogger<ESignatureAuthenticationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISmsService _smsService;
        private readonly IAuditService _auditService;
        private readonly ICryptoService _cryptoService;

        public ESignatureAuthenticationService(
            NexoraDbContext context,
            ILogger<ESignatureAuthenticationService> logger,
            IConfiguration configuration,
            ISmsService smsService,
            IAuditService auditService,
            ICryptoService cryptoService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _smsService = smsService;
            _auditService = auditService;
            _cryptoService = cryptoService;
        }

        public async Task<AuthenticationResult> InitiateOtpAuthenticationAsync(string phoneNumber, int documentId, int signerId, int tenantId)
        {
            try
            {
                _logger.LogInformation("Initiating OTP authentication for signer {SignerId} on document {DocumentId}", signerId, documentId);

                var signer = await _context.ESignatureSigners
                    .FirstOrDefaultAsync(s => s.Id == signerId && s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted);

                if (signer == null)
                    return new AuthenticationResult { IsSuccess = false, Message = "Signer not found" };

                var document = await _context.ESignatureDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                    return new AuthenticationResult { IsSuccess = false, Message = "Document not found" };

                var existingAuth = await _context.ESignatureAuthentications
                    .FirstOrDefaultAsync(a => a.SignerId == signerId && a.DocumentId == documentId && 
                                            a.AuthenticationType == "OTP" && a.Status == "Pending" && 
                                            a.ExpiresAt > DateTime.UtcNow && !a.IsDeleted);

                if (existingAuth != null)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = true,
                        Message = "OTP already sent",
                        TransactionId = existingAuth.TransactionId,
                        ExpiresAt = existingAuth.ExpiresAt,
                        AuthenticationId = existingAuth.Id
                    };
                }

                var otpCode = GenerateOtpCode();
                var transactionId = Guid.NewGuid().ToString("N");
                var expiresAt = DateTime.UtcNow.AddMinutes(5); // 5 minutes expiry

                var authentication = new ESignatureAuthentication
                {
                    TenantId = tenantId,
                    DocumentId = documentId,
                    SignerId = signerId,
                    AuthenticationType = "OTP",
                    Status = "Pending",
                    Identifier = phoneNumber,
                    TransactionId = transactionId,
                    Provider = "Internal",
                    Code = await _cryptoService.EncryptAsync(otpCode),
                    CodeGeneratedAt = DateTime.UtcNow,
                    CodeExpiresAt = expiresAt,
                    CodeLength = otpCode.Length,
                    MaxAttempts = 3,
                    AttemptCount = 0,
                    ExpiresAt = expiresAt,
                    Timestamp = DateTime.UtcNow
                };

                _context.ESignatureAuthentications.Add(authentication);
                await _context.SaveChangesAsync();

                var smsResult = await _smsService.SendSmsAsync(phoneNumber, $"Your e-signature verification code is: {otpCode}. Valid for 5 minutes.");

                if (smsResult != "SUCCESS")
                {
                    authentication.Status = "Failed";
                    authentication.FailureReason = "Failed to send SMS";
                    await _context.SaveChangesAsync();

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Failed to send OTP",
                        Errors = new List<string> { smsResult }
                    };
                }

                authentication.ProviderResponse = System.Text.Json.JsonSerializer.Serialize(smsResult);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    EntityName = nameof(ESignatureAuthentication),
                    EntityId = authentication.Id.ToString(),
                    Action = "InitiateOTP",
                    NewValues = new Dictionary<string, object?> { ["PhoneNumber"] = phoneNumber, ["TransactionId"] = transactionId },
                    Timestamp = DateTime.UtcNow
                });

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    Message = "OTP sent successfully",
                    TransactionId = transactionId,
                    ExpiresAt = expiresAt,
                    AuthenticationId = authentication.Id,
                    Provider = "Internal"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating OTP authentication for signer {SignerId}", signerId);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = "Internal error occurred",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<AuthenticationResult> VerifyOtpAuthenticationAsync(string phoneNumber, string code, int documentId, int signerId, int tenantId)
        {
            try
            {
                _logger.LogInformation("Verifying OTP authentication for signer {SignerId} on document {DocumentId}", signerId, documentId);

                var authentication = await _context.ESignatureAuthentications
                    .FirstOrDefaultAsync(a => a.SignerId == signerId && a.DocumentId == documentId && 
                                            a.AuthenticationType == "OTP" && a.Status == "Pending" && 
                                            a.Identifier == phoneNumber && !a.IsDeleted);

                if (authentication == null)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Authentication session not found",
                        RiskLevel = "High"
                    };
                }

                if (authentication.ExpiresAt <= DateTime.UtcNow)
                {
                    authentication.Status = "Expired";
                    await _context.SaveChangesAsync();

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "OTP has expired",
                        RiskLevel = "Medium"
                    };
                }

                if (authentication.AttemptCount >= authentication.MaxAttempts)
                {
                    authentication.Status = "Failed";
                    authentication.FailureReason = "Maximum attempts exceeded";
                    await _context.SaveChangesAsync();

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Maximum verification attempts exceeded",
                        RiskLevel = "High"
                    };
                }

                authentication.AttemptCount++;
                authentication.LastAttemptAt = DateTime.UtcNow;

                var storedCode = await _cryptoService.DecryptAsync(authentication.Code);
                if (storedCode != code)
                {
                    authentication.FailureReason = "Invalid OTP code";
                    await _context.SaveChangesAsync();

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Invalid OTP code",
                        RiskLevel = "Medium"
                    };
                }

                authentication.Status = "Verified";
                authentication.VerifiedAt = DateTime.UtcNow;
                authentication.IsComplianceVerified = true;
                authentication.ComplianceVerifiedAt = DateTime.UtcNow;
                authentication.ComplianceReference = Guid.NewGuid().ToString("N");

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    EntityName = nameof(ESignatureAuthentication),
                    EntityId = authentication.Id.ToString(),
                    Action = "VerifyOTP",
                    NewValues = new Dictionary<string, object?> { ["Status"] = "Verified", ["VerifiedAt"] = authentication.VerifiedAt },
                    Timestamp = DateTime.UtcNow
                });

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    Message = "OTP verified successfully",
                    AuthenticationId = authentication.Id,
                    RiskLevel = "Low"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP authentication for signer {SignerId}", signerId);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = "Internal error occurred",
                    Errors = new List<string> { ex.Message },
                    RiskLevel = "High"
                };
            }
        }

        public async Task<AuthenticationResult> InitiateNafathAuthenticationAsync(string nationalId, int documentId, int signerId, int tenantId)
        {
            try
            {
                _logger.LogInformation("Initiating Nafath authentication for signer {SignerId} on document {DocumentId}", signerId, documentId);

                var signer = await _context.ESignatureSigners
                    .FirstOrDefaultAsync(s => s.Id == signerId && s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted);

                if (signer == null)
                    return new AuthenticationResult { IsSuccess = false, Message = "Signer not found" };

                var nafathConfig = await GetAuthenticationConfigurationAsync("Nafath", tenantId);
                if (nafathConfig == null || !nafathConfig.ContainsKey("IsEnabled") || !(bool)nafathConfig["IsEnabled"])
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Nafath authentication is not enabled for this tenant"
                    };
                }

                var transactionId = Guid.NewGuid().ToString("N");
                var expiresAt = DateTime.UtcNow.AddMinutes(10); // 10 minutes for Nafath

                var authentication = new ESignatureAuthentication
                {
                    TenantId = tenantId,
                    DocumentId = documentId,
                    SignerId = signerId,
                    AuthenticationType = "Nafath",
                    Status = "Pending",
                    Identifier = nationalId,
                    TransactionId = transactionId,
                    Provider = "Nafath",
                    NationalId = nationalId,
                    NafathRequestedAt = DateTime.UtcNow,
                    NafathTransactionStatus = "Initiated",
                    ExpiresAt = expiresAt,
                    MaxAttempts = 1,
                    AttemptCount = 0,
                    Timestamp = DateTime.UtcNow
                };

                _context.ESignatureAuthentications.Add(authentication);
                await _context.SaveChangesAsync();

                var nafathResponse = await SimulateNafathInitiation(nationalId, transactionId);

                authentication.ProviderResponse = System.Text.Json.JsonSerializer.Serialize(nafathResponse);
                authentication.NafathData = System.Text.Json.JsonSerializer.Serialize(nafathResponse);

                if (!nafathResponse.IsSuccess)
                {
                    authentication.Status = "Failed";
                    authentication.FailureReason = nafathResponse.ErrorMessage;
                    await _context.SaveChangesAsync();

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = nafathResponse.ErrorMessage,
                        Errors = new List<string> { nafathResponse.ErrorMessage }
                    };
                }

                await _context.SaveChangesAsync();

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    Message = "Nafath authentication initiated. Please check your Nafath app.",
                    TransactionId = transactionId,
                    ExpiresAt = expiresAt,
                    AuthenticationId = authentication.Id,
                    Provider = "Nafath",
                    NextStep = "CheckStatus"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Nafath authentication for signer {SignerId}", signerId);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = "Internal error occurred",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        private string GenerateOtpCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private async Task<NafathResponse> SimulateNafathInitiation(string nationalId, string transactionId)
        {
            await Task.Delay(100);

            return new NafathResponse
            {
                IsSuccess = true,
                TransactionId = transactionId,
                Status = "Pending",
                Message = "Authentication request sent to user's device"
            };
        }

        private async Task<NafathResponse> SimulateNafathVerification(string transactionId, string code)
        {
            await Task.Delay(200);

            if (string.IsNullOrEmpty(code) || code.Length < 4)
            {
                return new NafathResponse
                {
                    IsSuccess = false,
                    TransactionId = transactionId,
                    Status = "Failed",
                    ErrorMessage = "Invalid verification code"
                };
            }

            return new NafathResponse
            {
                IsSuccess = true,
                TransactionId = transactionId,
                Status = "Completed",
                Message = "Nafath authentication verified successfully"
            };
        }

        private async Task<NafathResponse> SimulateNafathStatusCheck(string transactionId)
        {
            await Task.Delay(150);

            var random = new Random();
            var statusOptions = new[] { "Pending", "Completed", "Rejected" };
            var status = statusOptions[random.Next(statusOptions.Length)];

            return new NafathResponse
            {
                IsSuccess = status != "Rejected",
                TransactionId = transactionId,
                Status = status,
                Message = status switch
                {
                    "Pending" => "Waiting for user response",
                    "Completed" => "Authentication completed successfully",
                    "Rejected" => "User rejected the authentication request",
                    _ => "Unknown status"
                }
            };
        }

        public async Task<AuthenticationResult> ResendOtpAsync(string phoneNumber, int documentId, int signerId, int tenantId)
        {
            try
            {
                _logger.LogInformation("Resending OTP for signer {SignerId} on document {DocumentId}", signerId, documentId);

                var existingAuth = await _context.ESignatureAuthentications
                    .FirstOrDefaultAsync(a => a.SignerId == signerId && a.DocumentId == documentId && 
                                            a.AuthenticationType == "OTP" && a.Identifier == phoneNumber && 
                                            !a.IsDeleted);

                if (existingAuth == null)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "No existing OTP session found"
                    };
                }

                if (existingAuth.Status == "Verified")
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "OTP already verified"
                    };
                }

                var otpCode = GenerateOtpCode();
                var expiresAt = DateTime.UtcNow.AddMinutes(5);

                existingAuth.Code = await _cryptoService.EncryptAsync(otpCode);
                existingAuth.CodeGeneratedAt = DateTime.UtcNow;
                existingAuth.CodeExpiresAt = expiresAt;
                existingAuth.ExpiresAt = expiresAt;
                existingAuth.AttemptCount = 0;
                existingAuth.Status = "Pending";
                existingAuth.FailureReason = null;

                var smsResult = await _smsService.SendSmsAsync(phoneNumber, $"Your e-signature verification code is: {otpCode}. Valid for 5 minutes.");

                if (smsResult != "SUCCESS")
                {
                    existingAuth.Status = "Failed";
                    existingAuth.FailureReason = "Failed to send SMS";
                    await _context.SaveChangesAsync();

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Failed to resend OTP",
                        Errors = new List<string> { smsResult }
                    };
                }

                existingAuth.ProviderResponse = System.Text.Json.JsonSerializer.Serialize(smsResult);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = "System",
                    EntityName = nameof(ESignatureAuthentication),
                    EntityId = existingAuth.Id.ToString(),
                    Action = "ResendOTP",
                    NewValues = new Dictionary<string, object?> { ["PhoneNumber"] = phoneNumber },
                    Timestamp = DateTime.UtcNow
                });

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    Message = "OTP resent successfully",
                    TransactionId = existingAuth.TransactionId,
                    ExpiresAt = expiresAt,
                    AuthenticationId = existingAuth.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending OTP for signer {SignerId}", signerId);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = "Internal error occurred",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<AuthenticationResult> VerifyNafathAuthenticationAsync(string transactionId, string code, int documentId, int signerId, int tenantId)
        {
            try
            {
                _logger.LogInformation("Verifying Nafath authentication for transaction {TransactionId}", transactionId);

                var authentication = await _context.ESignatureAuthentications
                    .FirstOrDefaultAsync(a => a.TransactionId == transactionId && a.SignerId == signerId && 
                                            a.DocumentId == documentId && a.AuthenticationType == "Nafath" && 
                                            a.TenantId == tenantId && !a.IsDeleted);

                if (authentication == null)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Nafath authentication session not found",
                        RiskLevel = "High"
                    };
                }

                if (authentication.ExpiresAt <= DateTime.UtcNow)
                {
                    authentication.Status = "Expired";
                    authentication.NafathTransactionStatus = "Expired";
                    await _context.SaveChangesAsync();

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Nafath authentication has expired",
                        RiskLevel = "Medium"
                    };
                }

                var nafathVerificationResult = await SimulateNafathVerification(transactionId, code);

                authentication.AttemptCount++;
                authentication.LastAttemptAt = DateTime.UtcNow;
                authentication.NafathTransactionStatus = nafathVerificationResult.Status;

                if (!nafathVerificationResult.IsSuccess)
                {
                    authentication.Status = "Failed";
                    authentication.FailureReason = nafathVerificationResult.ErrorMessage;
                    await _context.SaveChangesAsync();

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = nafathVerificationResult.ErrorMessage,
                        RiskLevel = "High"
                    };
                }

                authentication.Status = "Verified";
                authentication.VerifiedAt = DateTime.UtcNow;
                authentication.IsComplianceVerified = true;
                authentication.ComplianceVerifiedAt = DateTime.UtcNow;
                authentication.ComplianceReference = Guid.NewGuid().ToString("N");
                authentication.NafathVerifiedAt = DateTime.UtcNow;
                authentication.NafathData = System.Text.Json.JsonSerializer.Serialize(nafathVerificationResult);

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    UserId = "System",
                    EntityName = nameof(ESignatureAuthentication),
                    EntityId = authentication.Id.ToString(),
                    Action = "VerifyNafath",
                    NewValues = new Dictionary<string, object?> { ["Status"] = "Verified", ["TransactionId"] = transactionId },
                    Timestamp = DateTime.UtcNow
                });

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    Message = "Nafath authentication verified successfully",
                    AuthenticationId = authentication.Id,
                    RiskLevel = "Low"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Nafath authentication for transaction {TransactionId}", transactionId);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = "Internal error occurred",
                    Errors = new List<string> { ex.Message },
                    RiskLevel = "High"
                };
            }
        }

        public async Task<AuthenticationResult> CheckNafathStatusAsync(string transactionId, int documentId, int signerId, int tenantId)
        {
            try
            {
                _logger.LogInformation("Checking Nafath status for transaction {TransactionId}", transactionId);

                var authentication = await _context.ESignatureAuthentications
                    .FirstOrDefaultAsync(a => a.TransactionId == transactionId && a.SignerId == signerId && 
                                            a.DocumentId == documentId && a.AuthenticationType == "Nafath" && 
                                            a.TenantId == tenantId && !a.IsDeleted);

                if (authentication == null)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Nafath authentication session not found"
                    };
                }

                if (authentication.Status == "Verified")
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = true,
                        Message = "Nafath authentication already verified",
                        AuthenticationId = authentication.Id
                    };
                }

                if (authentication.ExpiresAt <= DateTime.UtcNow)
                {
                    authentication.Status = "Expired";
                    authentication.NafathTransactionStatus = "Expired";
                    await _context.SaveChangesAsync();

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Nafath authentication has expired"
                    };
                }

                var nafathStatusResult = await SimulateNafathStatusCheck(transactionId);
                authentication.NafathTransactionStatus = nafathStatusResult.Status;

                if (nafathStatusResult.Status == "Completed")
                {
                    authentication.Status = "Verified";
                    authentication.VerifiedAt = DateTime.UtcNow;
                    authentication.IsComplianceVerified = true;
                    authentication.ComplianceVerifiedAt = DateTime.UtcNow;
                    authentication.ComplianceReference = Guid.NewGuid().ToString("N");
                    authentication.NafathVerifiedAt = DateTime.UtcNow;
                }
                else if (nafathStatusResult.Status == "Rejected" || nafathStatusResult.Status == "Failed")
                {
                    authentication.Status = "Failed";
                    authentication.FailureReason = "User rejected or failed Nafath authentication";
                }

                await _context.SaveChangesAsync();

                return new AuthenticationResult
                {
                    IsSuccess = nafathStatusResult.IsSuccess,
                    Message = nafathStatusResult.Message,
                    AuthenticationId = authentication.Id,
                    ExpiresAt = authentication.ExpiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Nafath status for transaction {TransactionId}", transactionId);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = "Internal error occurred",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public Task<AuthenticationResult> InitiateCertificateAuthenticationAsync(string certificateData, int documentId, int signerId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationResult> VerifyCertificateAuthenticationAsync(string certificateData, string signature, int documentId, int signerId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateDigitalCertificateAsync(string certificateData, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationResult> InitiateBiometricAuthenticationAsync(string biometricType, int documentId, int signerId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationResult> VerifyBiometricAuthenticationAsync(string biometricData, string biometricType, int documentId, int signerId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationResult> InitiatePasswordAuthenticationAsync(string email, int documentId, int signerId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationResult> VerifyPasswordAuthenticationAsync(string email, string password, int documentId, int signerId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsAuthenticationValidAsync(int authenticationId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsAuthenticationExpiredAsync(int authenticationId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<ESignatureAuthentication> GetAuthenticationAsync(int authenticationId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<List<ESignatureAuthentication>> GetDocumentAuthenticationsAsync(int documentId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<List<ESignatureAuthentication>> GetSignerAuthenticationsAsync(int signerId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateAuthenticationMethodAsync(string authenticationMethod, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetSupportedAuthenticationMethodsAsync(int tenantId)
        {
            throw new NotImplementedException();
        }

        public async Task<Dictionary<string, object>> GetAuthenticationConfigurationAsync(string authenticationMethod, int tenantId)
        {
            var config = new Dictionary<string, object>();

            switch (authenticationMethod.ToLower())
            {
                case "otp":
                    config["IsEnabled"] = true;
                    config["CodeLength"] = 6;
                    config["ExpiryMinutes"] = 5;
                    config["MaxAttempts"] = 3;
                    break;
                case "nafath":
                    config["IsEnabled"] = true;
                    config["ExpiryMinutes"] = 10;
                    config["MaxAttempts"] = 1;
                    break;
                case "certificate":
                    config["IsEnabled"] = true;
                    config["RequiredCertificateLevel"] = "Qualified";
                    break;
                case "biometric":
                    config["IsEnabled"] = false;
                    config["SupportedTypes"] = new[] { "Fingerprint", "FaceID" };
                    break;
                default:
                    return null;
            }

            return config;
        }

        public Task<AuthenticationResult> RefreshAuthenticationAsync(int authenticationId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RevokeAuthenticationAsync(int authenticationId, int tenantId, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<List<ESignatureAuthentication>> GetFailedAuthenticationAttemptsAsync(int documentId, int signerId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsSignerBlockedAsync(int signerId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<DateTime?> GetSignerBlockExpiryAsync(int signerId, int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, object>> GetAuthenticationMetricsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            throw new NotImplementedException();
        }

        public Task<List<AuthenticationSecurityEvent>> GetSecurityEventsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            throw new NotImplementedException();
        }

        public Task ProcessExpiredAuthenticationsAsync(int tenantId)
        {
            throw new NotImplementedException();
        }

        public Task CleanupOldAuthenticationDataAsync(int tenantId, TimeSpan retentionPeriod)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ValidateSignerAuthenticationAsync(int signerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var signer = await _context.ESignatureSigners.FindAsync(signerId);
                if (signer == null) return false;

                switch (signer.AuthenticationMethod?.ToLower())
                {
                    case "otp":
                        return await ValidateOtpAuthenticationAsync(signer, cancellationToken);
                    case "nafath":
                        return await ValidateNafathAuthenticationAsync(signer, cancellationToken);
                    case "password":
                        return await ValidatePasswordAuthenticationAsync(signer, cancellationToken);
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating signer authentication for signer {SignerId}", signerId);
                return false;
            }
        }

        private async Task<bool> ValidateOtpAuthenticationAsync(ESignatureSigner signer, CancellationToken cancellationToken)
        {
            var authentication = await _context.ESignatureAuthentications
                .FirstOrDefaultAsync(a => a.SignerId == signer.Id && 
                                        a.AuthenticationType == "OTP" && 
                                        a.Status == "Verified" && 
                                        !a.IsDeleted, cancellationToken);
            return authentication != null;
        }

        private async Task<bool> ValidateNafathAuthenticationAsync(ESignatureSigner signer, CancellationToken cancellationToken)
        {
            var authentication = await _context.ESignatureAuthentications
                .FirstOrDefaultAsync(a => a.SignerId == signer.Id && 
                                        a.AuthenticationType == "Nafath" && 
                                        a.Status == "Verified" && 
                                        !a.IsDeleted, cancellationToken);
            return authentication != null;
        }

        private async Task<bool> ValidatePasswordAuthenticationAsync(ESignatureSigner signer, CancellationToken cancellationToken)
        {
            return !string.IsNullOrEmpty(signer.AuthenticationData);
        }
    }

    public class NafathResponse
    {
        public bool IsSuccess { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }


}
