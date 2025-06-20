using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using System.Text.Json;
using System.Text;

namespace Nexora.Core.Security;

public interface IGdprComplianceService
{
    Task<GdprDataExportResult> ExportUserDataAsync(string userId);
    Task<GdprDeletionResult> DeleteUserDataAsync(string userId, bool hardDelete = false);
    Task<GdprAnonymizationResult> AnonymizeUserDataAsync(string userId);
    Task<GdprConsentResult> UpdateConsentAsync(string userId, GdprConsentRequest consent);
    Task<GdprConsentStatus> GetConsentStatusAsync(string userId);
    Task<bool> ValidateDataRetentionAsync();
}

public class GdprComplianceService : IGdprComplianceService
{
    private readonly IUserRepository _userRepository;
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IRepository<AuditLog> _auditRepository;
    private readonly IComprehensiveAuditService _auditService;
    private readonly ILogger<GdprComplianceService> _logger;
    private readonly ITenantService _tenantService;

    public GdprComplianceService(
        IUserRepository userRepository,
        IRepository<Transaction> transactionRepository,
        IRepository<Payment> paymentRepository,
        IRepository<AuditLog> auditRepository,
        IComprehensiveAuditService auditService,
        ILogger<GdprComplianceService> logger,
        ITenantService tenantService)
    {
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _paymentRepository = paymentRepository;
        _auditRepository = auditRepository;
        _auditService = auditService;
        _logger = logger;
        _tenantService = tenantService;
    }

    public async Task<GdprDataExportResult> ExportUserDataAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(int.Parse(userId));
            if (user == null)
                return new GdprDataExportResult { Success = false, Error = "User not found" };

            var userData = new
            {
                PersonalInformation = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.PhoneNumber,
                    user.CreatedAt,
                    user.UpdatedAt,
                    user.LastLoginAt
                },
                Transactions = await GetUserTransactionsAsync(userId),
                Payments = await GetUserPaymentsAsync(userId),
                AuditLogs = await GetUserAuditLogsAsync(userId),
                ConsentHistory = await GetUserConsentHistoryAsync(userId)
            };

            var jsonData = JsonSerializer.Serialize(userData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await _auditService.LogUserActionAsync("GDPR_DATA_EXPORT", new { UserId = userId }, userId);

            return new GdprDataExportResult
            {
                Success = true,
                Data = jsonData,
                ExportDate = DateTime.UtcNow,
                UserId = userId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting user data for GDPR compliance: {UserId}", userId);
            return new GdprDataExportResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<GdprDeletionResult> DeleteUserDataAsync(string userId, bool hardDelete = false)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(int.Parse(userId));
            if (user == null)
                return new GdprDeletionResult { Success = false, Error = "User not found" };

            var deletionSummary = new List<string>();

            if (hardDelete)
            {
                await DeleteUserTransactionsAsync(userId);
                deletionSummary.Add("Transactions permanently deleted");

                await DeleteUserPaymentsAsync(userId);
                deletionSummary.Add("Payments permanently deleted");

                await DeleteUserAuditLogsAsync(userId);
                deletionSummary.Add("Audit logs permanently deleted");

                await _userRepository.DeleteAsync(user);
                deletionSummary.Add("User account permanently deleted");
            }
            else
            {
                await AnonymizeUserDataInternalAsync(userId);
                deletionSummary.Add("User data anonymized");

                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                user.Email = $"deleted-{Guid.NewGuid()}@anonymized.local";
                user.FirstName = "Deleted";
                user.LastName = "User";
                user.PhoneNumber = null;

                await _userRepository.UpdateAsync(user);
                deletionSummary.Add("User account soft deleted");
            }

            await _userRepository.SaveChangesAsync();

            await _auditService.LogUserActionAsync("GDPR_DATA_DELETION", 
                new { UserId = userId, HardDelete = hardDelete, Summary = deletionSummary }, userId);

            return new GdprDeletionResult
            {
                Success = true,
                DeletionDate = DateTime.UtcNow,
                UserId = userId,
                HardDelete = hardDelete,
                Summary = deletionSummary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user data for GDPR compliance: {UserId}", userId);
            return new GdprDeletionResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<GdprAnonymizationResult> AnonymizeUserDataAsync(string userId)
    {
        try
        {
            await AnonymizeUserDataInternalAsync(userId);

            await _auditService.LogUserActionAsync("GDPR_DATA_ANONYMIZATION", 
                new { UserId = userId }, userId);

            return new GdprAnonymizationResult
            {
                Success = true,
                AnonymizationDate = DateTime.UtcNow,
                UserId = userId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error anonymizing user data for GDPR compliance: {UserId}", userId);
            return new GdprAnonymizationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<GdprConsentResult> UpdateConsentAsync(string userId, GdprConsentRequest consent)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(int.Parse(userId));
            if (user == null)
                return new GdprConsentResult { Success = false, Error = "User not found" };

            var consentData = new
            {
                UserId = userId,
                MarketingConsent = consent.MarketingConsent,
                AnalyticsConsent = consent.AnalyticsConsent,
                DataProcessingConsent = consent.DataProcessingConsent,
                ConsentDate = DateTime.UtcNow,
                IpAddress = consent.IpAddress,
                UserAgent = consent.UserAgent
            };

            await _auditService.LogUserActionAsync("GDPR_CONSENT_UPDATE", consentData, userId);

            return new GdprConsentResult
            {
                Success = true,
                ConsentDate = DateTime.UtcNow,
                UserId = userId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GDPR consent: {UserId}", userId);
            return new GdprConsentResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<GdprConsentStatus> GetConsentStatusAsync(string userId)
    {
        try
        {
            return new GdprConsentStatus
            {
                UserId = userId,
                MarketingConsent = false,
                AnalyticsConsent = false,
                DataProcessingConsent = true,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GDPR consent status: {UserId}", userId);
            return new GdprConsentStatus { UserId = userId };
        }
    }

    public async Task<bool> ValidateDataRetentionAsync()
    {
        try
        {
            var retentionPeriod = TimeSpan.FromDays(2555); // 7 years for financial data
            var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);

            var allAuditLogs = await _auditRepository.GetAllAsync();
            var expiredAuditLogs = allAuditLogs.Where(a => a.Timestamp < cutoffDate);
            
            if (expiredAuditLogs.Any())
            {
                _logger.LogWarning("Found {Count} audit logs exceeding retention period", expiredAuditLogs.Count());
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating data retention");
            return false;
        }
    }

    private async Task<object> GetUserTransactionsAsync(string userId)
    {
        var allTransactions = await _transactionRepository.GetAllAsync();
        var transactions = allTransactions.Where(t => t.UserId == int.Parse(userId));
        return transactions.Select(t => new
        {
            t.Id,
            t.ReferenceId,
            t.Amount,
            t.Currency,
            t.Status,
            t.Type,
            t.Description,
            t.CreatedAt,
            t.UpdatedAt
        });
    }

    private async Task<object> GetUserPaymentsAsync(string userId)
    {
        var allPayments = await _paymentRepository.GetAllAsync();
        var payments = allPayments.Where(p => p.UserId == int.Parse(userId));
        return payments.Select(p => new
        {
            p.Id,
            p.ReferenceId,
            p.Amount,
            p.Currency,
            p.Status,
            p.PaymentMethod,
            p.CreatedAt,
            p.UpdatedAt
        });
    }

    private async Task<object> GetUserAuditLogsAsync(string userId)
    {
        var allAuditLogs = await _auditRepository.GetAllAsync();
        var auditLogs = allAuditLogs.Where(a => a.UserId == userId);
        return auditLogs.Select(a => new
        {
            a.Id,
            a.EventType,
            a.Action,
            a.Description,
            a.Timestamp,
            a.IpAddress
        });
    }

    private async Task<object> GetUserConsentHistoryAsync(string userId)
    {
        return new List<object>();
    }

    private async Task AnonymizeUserDataInternalAsync(string userId)
    {
        var allTransactions = await _transactionRepository.GetAllAsync();
        var transactions = allTransactions.Where(t => t.UserId == int.Parse(userId));
        foreach (var transaction in transactions)
        {
            transaction.Description = "Anonymized transaction";
            await _transactionRepository.UpdateAsync(transaction);
        }

        var allPayments = await _paymentRepository.GetAllAsync();
        var payments = allPayments.Where(p => p.UserId == int.Parse(userId));
        foreach (var payment in payments)
        {
            payment.PaymentMethod = "Anonymized";
            await _paymentRepository.UpdateAsync(payment);
        }
    }

    private async Task DeleteUserTransactionsAsync(string userId)
    {
        var allTransactions = await _transactionRepository.GetAllAsync();
        var transactions = allTransactions.Where(t => t.UserId == int.Parse(userId));
        foreach (var transaction in transactions)
        {
            await _transactionRepository.DeleteAsync(transaction);
        }
    }

    private async Task DeleteUserPaymentsAsync(string userId)
    {
        var allPayments = await _paymentRepository.GetAllAsync();
        var payments = allPayments.Where(p => p.UserId == int.Parse(userId));
        foreach (var payment in payments)
        {
            await _paymentRepository.DeleteAsync(payment);
        }
    }

    private async Task DeleteUserAuditLogsAsync(string userId)
    {
        var allAuditLogs = await _auditRepository.GetAllAsync();
        var auditLogs = allAuditLogs.Where(a => a.UserId == userId);
        foreach (var auditLog in auditLogs)
        {
            await _auditRepository.DeleteAsync(auditLog);
        }
    }
}

public class GdprDataExportResult
{
    public bool Success { get; set; }
    public string Data { get; set; }
    public DateTime ExportDate { get; set; }
    public string UserId { get; set; }
    public string Error { get; set; }
}

public class GdprDeletionResult
{
    public bool Success { get; set; }
    public DateTime DeletionDate { get; set; }
    public string UserId { get; set; }
    public bool HardDelete { get; set; }
    public List<string> Summary { get; set; } = new();
    public string Error { get; set; }
}

public class GdprAnonymizationResult
{
    public bool Success { get; set; }
    public DateTime AnonymizationDate { get; set; }
    public string UserId { get; set; }
    public string Error { get; set; }
}

public class GdprConsentResult
{
    public bool Success { get; set; }
    public DateTime ConsentDate { get; set; }
    public string UserId { get; set; }
    public string Error { get; set; }
}

public class GdprConsentRequest
{
    public bool MarketingConsent { get; set; }
    public bool AnalyticsConsent { get; set; }
    public bool DataProcessingConsent { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
}

public class GdprConsentStatus
{
    public string UserId { get; set; }
    public bool MarketingConsent { get; set; }
    public bool AnalyticsConsent { get; set; }
    public bool DataProcessingConsent { get; set; }
    public DateTime LastUpdated { get; set; }
}

public static class GdprComplianceExtensions
{
    public static IServiceCollection AddGdprCompliance(this IServiceCollection services)
    {
        services.AddScoped<IGdprComplianceService, GdprComplianceService>();
        return services;
    }
}
