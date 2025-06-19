using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexora.Core.Security;

public interface IPaymentSecurityService
{
    Task<PaymentSecurityResult> ValidatePaymentSecurityAsync(Payment payment, PaymentContext context);
    Task<bool> IsGeographicallyAllowedAsync(string country, string paymentMethod);
    Task<PciComplianceResult> ValidatePciComplianceAsync(Payment payment);
    Task<VelocityCheckResult> CheckPaymentVelocityAsync(string userId, decimal amount);
    Task<CardValidationResult> ValidateCardSecurityAsync(string cardToken, PaymentContext context);
}

public class PaymentSecurityService : IPaymentSecurityService
{
    private readonly ILogger<PaymentSecurityService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IRepository<User> _userRepository;

    public PaymentSecurityService(
        ILogger<PaymentSecurityService> logger,
        IConfiguration configuration,
        IRepository<Payment> paymentRepository,
        IRepository<User> userRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _paymentRepository = paymentRepository;
        _userRepository = userRepository;
    }

    public async Task<PaymentSecurityResult> ValidatePaymentSecurityAsync(Payment payment, PaymentContext context)
    {
        var result = new PaymentSecurityResult { IsValid = true };
        var violations = new List<string>();

        try
        {
            if (!await IsGeographicallyAllowedAsync(context.Country, payment.PaymentMethod))
            {
                violations.Add("Payment not allowed from this geographic location");
                result.RiskScore += 0.8;
            }

            var pciResult = await ValidatePciComplianceAsync(payment);
            if (!pciResult.IsCompliant)
            {
                violations.AddRange(pciResult.Violations);
                result.RiskScore += 0.6;
            }

            var velocityResult = await CheckPaymentVelocityAsync(payment.UserId.ToString(), payment.Amount);
            if (!velocityResult.IsWithinLimits)
            {
                violations.AddRange(velocityResult.Violations);
                result.RiskScore += velocityResult.RiskScore;
            }

            if (!string.IsNullOrEmpty(context.CardToken))
            {
                var cardResult = await ValidateCardSecurityAsync(context.CardToken, context);
                if (!cardResult.IsValid)
                {
                    violations.AddRange(cardResult.Violations);
                    result.RiskScore += cardResult.RiskScore;
                }
            }

            if (payment.Amount > 50000) // High-value transaction
            {
                violations.Add("High-value transaction requires additional verification");
                result.RiskScore += 0.3;
            }

            if (IsOffHours(payment.CreatedAt))
            {
                violations.Add("Transaction attempted outside normal business hours");
                result.RiskScore += 0.2;
            }

            result.Violations = violations;
            result.IsValid = result.RiskScore < 0.8 && violations.Count == 0;
            result.RequiresManualReview = result.RiskScore >= 0.6;

            _logger.LogInformation("Payment security validation completed for payment {PaymentId}: Valid={IsValid}, RiskScore={RiskScore}", 
                payment.Id, result.IsValid, result.RiskScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating payment security for payment {PaymentId}", payment.Id);
            return new PaymentSecurityResult
            {
                IsValid = false,
                RiskScore = 1.0,
                Violations = new List<string> { "Security validation failed - manual review required" },
                RequiresManualReview = true
            };
        }
    }

    public async Task<bool> IsGeographicallyAllowedAsync(string country, string paymentMethod)
    {
        await Task.CompletedTask;

        var blockedCountries = _configuration.GetSection("PaymentSecurity:BlockedCountries").Get<string[]>() 
            ?? new[] { "AF", "IR", "KP", "SY", "YE" };

        if (blockedCountries.Contains(country?.ToUpper()))
            return false;

        if (paymentMethod?.ToLower() == "crypto" && country?.ToUpper() != "SA")
            return false;

        var allowedCountries = new[] { "SA", "AE", "KW", "QA", "BH", "OM", "US", "GB", "DE", "FR", "CA", "AU" };
        return allowedCountries.Contains(country?.ToUpper());
    }

    public async Task<PciComplianceResult> ValidatePciComplianceAsync(Payment payment)
    {
        await Task.CompletedTask;

        var result = new PciComplianceResult { IsCompliant = true };
        var violations = new List<string>();

        if (ContainsSensitiveCardData(payment.ExternalReference))
        {
            violations.Add("Payment reference contains potentially sensitive card data");
        }

        if (string.IsNullOrEmpty(payment.IdempotencyKey))
        {
            violations.Add("Missing idempotency key for payment security");
        }

        var trustedProviders = new[] { "stripe", "paypal", "mada", "hyperpay" };
        if (!trustedProviders.Contains(payment.PaymentMethod?.ToLower()))
        {
            violations.Add($"Payment provider '{payment.PaymentMethod}' not in trusted provider list");
        }

        if (payment.Amount > 100000) // SAR 100,000 limit
        {
            violations.Add("Payment amount exceeds PCI compliance threshold");
        }

        result.Violations = violations;
        result.IsCompliant = violations.Count == 0;

        return result;
    }

    public async Task<VelocityCheckResult> CheckPaymentVelocityAsync(string userId, decimal amount)
    {
        var result = new VelocityCheckResult { IsWithinLimits = true };
        var violations = new List<string>();
        var now = DateTime.UtcNow;

        var recentPayments = await _paymentRepository.GetAsync(p => 
            p.UserId.ToString() == userId && 
            p.CreatedAt >= now.AddHours(-24));

        var lastHour = recentPayments.Where(p => p.CreatedAt >= now.AddHours(-1));
        var hourlyCount = lastHour.Count();
        var hourlyAmount = lastHour.Sum(p => p.Amount);

        if (hourlyCount >= 5)
        {
            violations.Add("Exceeded hourly payment count limit (5 payments)");
            result.RiskScore += 0.4;
        }

        if (hourlyAmount + amount > 25000)
        {
            violations.Add("Exceeded hourly payment amount limit (SAR 25,000)");
            result.RiskScore += 0.5;
        }

        var dailyCount = recentPayments.Count();
        var dailyAmount = recentPayments.Sum(p => p.Amount);

        if (dailyCount >= 20)
        {
            violations.Add("Exceeded daily payment count limit (20 payments)");
            result.RiskScore += 0.3;
        }

        if (dailyAmount + amount > 100000)
        {
            violations.Add("Exceeded daily payment amount limit (SAR 100,000)");
            result.RiskScore += 0.4;
        }

        var failedPayments = recentPayments.Where(p => p.Status == "Failed" || p.Status == "Declined");
        if (failedPayments.Count() > 3)
        {
            violations.Add("Too many failed payment attempts in 24 hours");
            result.RiskScore += 0.6;
        }

        result.Violations = violations;
        result.IsWithinLimits = violations.Count == 0;

        return result;
    }

    public async Task<CardValidationResult> ValidateCardSecurityAsync(string cardToken, PaymentContext context)
    {
        await Task.CompletedTask;

        var result = new CardValidationResult { IsValid = true };
        var violations = new List<string>();

        if (ContainsRawCardNumber(cardToken))
        {
            violations.Add("Raw card number detected - must use tokenized format");
            result.RiskScore += 1.0;
        }

        if (context.Metadata?.Contains("cvv") == true || context.Metadata?.Contains("cvc") == true)
        {
            violations.Add("CVV/CVC data must not be stored or transmitted");
            result.RiskScore += 1.0;
        }

        if (!string.IsNullOrEmpty(context.CardBin))
        {
            if (!IsValidCardBin(context.CardBin))
            {
                violations.Add("Invalid or suspicious card BIN detected");
                result.RiskScore += 0.4;
            }
        }

        if (context.IsNewCard && context.Amount < 10) // Small amount on new card
        {
            violations.Add("Potential card testing detected - small amount on new card");
            result.RiskScore += 0.3;
        }

        result.Violations = violations;
        result.IsValid = violations.Count == 0 && result.RiskScore < 0.8;

        return result;
    }

    private bool ContainsSensitiveCardData(string data)
    {
        if (string.IsNullOrEmpty(data)) return false;

        var cardPattern = @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b";
        return Regex.IsMatch(data, cardPattern);
    }

    private bool ContainsRawCardNumber(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;

        var rawCardPattern = @"^\d{13,19}$";
        return Regex.IsMatch(token, rawCardPattern);
    }

    private bool IsValidCardBin(string bin)
    {
        if (string.IsNullOrEmpty(bin) || bin.Length < 6) return false;

        var binNumber = bin.Substring(0, 6);
        
        if (binNumber.StartsWith("4")) return true;
        
        if (binNumber.StartsWith("5") || binNumber.StartsWith("2")) return true;
        
        if (binNumber.StartsWith("34") || binNumber.StartsWith("37")) return true;
        
        var madaBins = new[] { "440647", "440795", "446404", "457865", "968208", "968209" };
        if (madaBins.Any(madaBin => binNumber.StartsWith(madaBin.Substring(0, Math.Min(6, madaBin.Length)))))
            return true;

        return false;
    }

    private bool IsOffHours(DateTime timestamp)
    {
        var hour = timestamp.Hour;
        return hour < 8 || hour > 22;
    }
}

public class PaymentSecurityResult
{
    public bool IsValid { get; set; }
    public double RiskScore { get; set; }
    public List<string> Violations { get; set; } = new();
    public bool RequiresManualReview { get; set; }
}

public class PciComplianceResult
{
    public bool IsCompliant { get; set; }
    public List<string> Violations { get; set; } = new();
}

public class VelocityCheckResult
{
    public bool IsWithinLimits { get; set; }
    public double RiskScore { get; set; }
    public List<string> Violations { get; set; } = new();
}

public class CardValidationResult
{
    public bool IsValid { get; set; }
    public double RiskScore { get; set; }
    public List<string> Violations { get; set; } = new();
}

public class PaymentContext
{
    public string Country { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string CardToken { get; set; } = string.Empty;
    public string CardBin { get; set; } = string.Empty;
    public bool IsNewCard { get; set; }
    public decimal Amount { get; set; }
    public string Metadata { get; set; } = string.Empty;
}
