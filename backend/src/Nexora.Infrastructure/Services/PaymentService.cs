using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Nexora.Core.Data;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using static Nexora.Core.Security.PaymentSecurityService;

namespace Nexora.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly NexoraDbContext _dbContext;
        private readonly ILogger<PaymentService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly ITenantService _tenantService;
        private readonly IPaymentSecurityService _paymentSecurityService;

        public PaymentService(
            NexoraDbContext dbContext,
            ILogger<PaymentService> logger,
            ICacheService cacheService,
            IConfiguration configuration,
            ITenantService tenantService,
            IPaymentSecurityService paymentSecurityService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _cacheService = cacheService;
            _configuration = configuration;
            _tenantService = tenantService;
            _paymentSecurityService = paymentSecurityService;
        }

        public async Task<Payment> ProcessPaymentAsync(PaymentRequest request)
        {
            // Validate request
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrEmpty(request.IdempotencyKey))
            {
                throw new ArgumentException("Idempotency key is required", nameof(request.IdempotencyKey));
            }

            // Check for existing payment with the same idempotency key
            var existingPayment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.IdempotencyKey == request.IdempotencyKey);

            if (existingPayment != null)
            {
                _logger.LogInformation("Payment with idempotency key {IdempotencyKey} already exists", request.IdempotencyKey);
                return existingPayment;
            }

            // Get transaction
            var transaction = await _dbContext.Transactions.FindAsync(request.TransactionId);
            if (transaction == null)
            {
                throw new ArgumentException($"Transaction with ID {request.TransactionId} not found", nameof(request.TransactionId));
            }

            // Create payment
            var payment = new Payment
            {
                TransactionId = request.TransactionId,
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = "Pending",
                IdempotencyKey = request.IdempotencyKey,
                CreatedAt = DateTime.UtcNow,
                TenantId = _tenantService.GetCurrentTenantId()
            };

            try
            {
                // Create payment context for security validation
                var paymentContext = new PaymentContext
                {
                    Amount = payment.Amount,
                    Country = ExtractCountryFromRequest(request),
                    IpAddress = ExtractIpFromRequest(request),
                    CardToken = request.ExternalReference,
                    IsNewCard = await IsNewCardAsync(payment.PaymentMethod, request.ExternalReference),
                    Metadata = request.Metadata ?? string.Empty
                };

                var securityResult = await _paymentSecurityService.ValidatePaymentSecurityAsync(payment, paymentContext);
                
                if (!securityResult.IsValid)
                {
                    payment.Status = "Blocked";
                    payment.Metadata = JsonSerializer.Serialize(new { 
                        securityViolations = securityResult.Violations,
                        riskScore = securityResult.RiskScore 
                    });
                    
                    await _dbContext.Payments.AddAsync(payment);
                    await _dbContext.SaveChangesAsync();
                    
                    _logger.LogWarning("Payment {PaymentId} blocked due to security violations: {Violations}", 
                        payment.Id, string.Join(", ", securityResult.Violations));
                    
                    throw new InvalidOperationException($"Payment blocked: {string.Join(", ", securityResult.Violations)}");
                }

                if (securityResult.RequiresManualReview)
                {
                    payment.Status = "PendingReview";
                    payment.Metadata = JsonSerializer.Serialize(new { 
                        requiresManualReview = true,
                        riskScore = securityResult.RiskScore,
                        securityFactors = securityResult.Violations
                    });
                    
                    await _dbContext.Payments.AddAsync(payment);
                    await _dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("Payment {PaymentId} requires manual review due to security risk score: {RiskScore}", 
                        payment.Id, securityResult.RiskScore);
                    
                    return payment;
                }

                // Process payment with external provider
                var result = await ProcessWithExternalProviderAsync(payment);
                
                // Update payment status
                payment.Status = result.Success ? "Completed" : "Failed";
                payment.ExternalReference = result.ReferenceId;
                
                // Update transaction status
                if (result.Success)
                {
                    transaction.Status = "Paid";
                    transaction.UpdatedAt = DateTime.UtcNow;
                }
                
                // Save payment
                await _dbContext.Payments.AddAsync(payment);
                await _dbContext.SaveChangesAsync();
                
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for transaction {TransactionId}", request.TransactionId);
                
                // Save failed payment
                payment.Status = "Failed";
                await _dbContext.Payments.AddAsync(payment);
                await _dbContext.SaveChangesAsync();
                
                throw;
            }
        }

        public async Task<Payment> RefundPaymentAsync(RefundRequest request)
        {
            // Validate request
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrEmpty(request.IdempotencyKey))
            {
                throw new ArgumentException("Idempotency key is required", nameof(request.IdempotencyKey));
            }

            // Check for existing refund with the same idempotency key
            var existingRefund = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.IdempotencyKey == request.IdempotencyKey);

            if (existingRefund != null)
            {
                _logger.LogInformation("Refund with idempotency key {IdempotencyKey} already exists", request.IdempotencyKey);
                return existingRefund;
            }

            // Get original payment
            var originalPayment = await _dbContext.Payments.FindAsync(request.PaymentId);
            if (originalPayment == null)
            {
                throw new ArgumentException($"Payment with ID {request.PaymentId} not found", nameof(request.PaymentId));
            }

            if (originalPayment.Status != "Completed")
            {
                throw new InvalidOperationException($"Cannot refund payment with status {originalPayment.Status}");
            }

            // Get transaction
            var transaction = await _dbContext.Transactions.FindAsync(originalPayment.TransactionId);
            if (transaction == null)
            {
                throw new ArgumentException($"Transaction not found for payment {request.PaymentId}");
            }

            // Create refund payment
            var refund = new Payment
            {
                TransactionId = originalPayment.TransactionId,
                PaymentMethod = originalPayment.PaymentMethod,
                Amount = request.Amount ?? originalPayment.Amount,
                Currency = originalPayment.Currency,
                Status = "Pending",
                IdempotencyKey = request.IdempotencyKey,
                CreatedAt = DateTime.UtcNow,
                RefundForPaymentId = originalPayment.Id
            };

            try
            {
                // Process refund with external provider
                var result = await ProcessRefundWithExternalProviderAsync(refund, originalPayment);
                
                // Update refund status
                refund.Status = result.Success ? "Completed" : "Failed";
                refund.ExternalReference = result.ReferenceId;
                
                // Update transaction status if full refund
                if (result.Success && refund.Amount == originalPayment.Amount)
                {
                    transaction.Status = "Refunded";
                    transaction.UpdatedAt = DateTime.UtcNow;
                }
                else if (result.Success)
                {
                    transaction.Status = "PartiallyRefunded";
                    transaction.UpdatedAt = DateTime.UtcNow;
                }
                
                // Save refund
                await _dbContext.Payments.AddAsync(refund);
                await _dbContext.SaveChangesAsync();
                
                return refund;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for payment {PaymentId}", request.PaymentId);
                
                // Save failed refund
                refund.Status = "Failed";
                await _dbContext.Payments.AddAsync(refund);
                await _dbContext.SaveChangesAsync();
                
                throw;
            }
        }

        private async Task<PaymentResult> ProcessWithExternalProviderAsync(Payment payment)
        {
            // Simulate external payment processing
            await Task.Delay(100);
            
            return new PaymentResult
            {
                Success = true,
                ReferenceId = Guid.NewGuid().ToString("N")
            };
        }

        private async Task<PaymentResult> ProcessRefundWithExternalProviderAsync(Payment refund, Payment originalPayment)
        {
            // Simulate external refund processing
            await Task.Delay(100);
            
            return new PaymentResult
            {
                Success = true,
                ReferenceId = Guid.NewGuid().ToString("N")
            };
        }

        public async Task<Subscription> CreateSubscriptionAsync(CreateSubscriptionRequest request)
        {
            try
            {
                _logger.LogInformation("Creating subscription for tenant {TenantId}", _tenantService.GetCurrentTenantId());

                if (string.IsNullOrEmpty(request.IdempotencyKey))
                {
                    throw new ArgumentException("Idempotency key is required", nameof(request.IdempotencyKey));
                }

                var existingSubscription = await _dbContext.Subscriptions
                    .FirstOrDefaultAsync(s => s.IdempotencyKey == request.IdempotencyKey);

                if (existingSubscription != null)
                {
                    _logger.LogInformation("Subscription with idempotency key {IdempotencyKey} already exists", request.IdempotencyKey);
                    return existingSubscription;
                }

                var subscription = new Subscription
                {
                    TenantId = _tenantService.GetCurrentTenantId(),
                    PlanName = request.PlanName,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    BillingCycle = request.BillingCycle,
                    Status = "Active",
                    StartDate = request.StartDate ?? DateTime.UtcNow,
                    NextBillingDate = CalculateNextBillingDate(request.StartDate ?? DateTime.UtcNow, request.BillingCycle),
                    PaymentMethod = request.PaymentMethod,
                    IdempotencyKey = request.IdempotencyKey,
                    CreatedAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };

                await _dbContext.Subscriptions.AddAsync(subscription);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Subscription {SubscriptionId} created successfully", subscription.Id);
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                throw;
            }
        }

        public async Task<Payment> ProcessRecurringPaymentAsync(int subscriptionId)
        {
            try
            {
                var subscription = await _dbContext.Subscriptions.FindAsync(subscriptionId);
                if (subscription == null)
                {
                    throw new ArgumentException($"Subscription with ID {subscriptionId} not found");
                }

                if (subscription.Status != "Active")
                {
                    throw new InvalidOperationException($"Cannot process payment for subscription with status {subscription.Status}");
                }

                var paymentRequest = new PaymentRequest
                {
                    TransactionId = 0, // Will be created
                    PaymentMethod = subscription.PaymentMethod,
                    Amount = subscription.Amount,
                    Currency = subscription.Currency,
                    IdempotencyKey = $"recurring_{subscription.Id}_{DateTime.UtcNow:yyyyMMdd}"
                };

                var transaction = new Transaction
                {
                    TenantId = subscription.TenantId,
                    Amount = subscription.Amount,
                    Currency = subscription.Currency,
                    Type = "Subscription",
                    Status = "Pending",
                    Description = $"Recurring payment for {subscription.PlanName}",
                    CreatedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        { "subscriptionId", subscription.Id },
                        { "billingCycle", subscription.BillingCycle }
                    }
                };

                await _dbContext.Transactions.AddAsync(transaction);
                await _dbContext.SaveChangesAsync();

                paymentRequest.TransactionId = transaction.Id;
                var payment = await ProcessPaymentAsync(paymentRequest);

                if (payment.Status == "Completed")
                {
                    subscription.NextBillingDate = CalculateNextBillingDate(subscription.NextBillingDate, subscription.BillingCycle);
                    subscription.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring payment for subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        public async Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            try
            {
                if (fromCurrency == toCurrency)
                    return amount;

                _logger.LogInformation("Converting {Amount} from {FromCurrency} to {ToCurrency}", amount, fromCurrency, toCurrency);

                var cacheKey = $"exchange_rate_{fromCurrency}_{toCurrency}";
                var cachedRate = await _cacheService.GetAsync<decimal?>(cacheKey);

                decimal exchangeRate;
                if (cachedRate.HasValue)
                {
                    exchangeRate = cachedRate.Value;
                }
                else
                {
                    exchangeRate = await GetExchangeRateAsync(fromCurrency, toCurrency);
                    await _cacheService.SetAsync(cacheKey, exchangeRate, TimeSpan.FromMinutes(15));
                }

                var convertedAmount = amount * exchangeRate;
                _logger.LogInformation("Converted {Amount} {FromCurrency} to {ConvertedAmount} {ToCurrency} (rate: {Rate})", 
                    amount, fromCurrency, convertedAmount, toCurrency, exchangeRate);

                return Math.Round(convertedAmount, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting currency from {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);
                throw;
            }
        }

        public async Task<ScheduledPayment> SchedulePaymentAsync(SchedulePaymentRequest request)
        {
            try
            {
                _logger.LogInformation("Scheduling payment for {ScheduledDate}", request.ScheduledDate);

                if (string.IsNullOrEmpty(request.IdempotencyKey))
                {
                    throw new ArgumentException("Idempotency key is required", nameof(request.IdempotencyKey));
                }

                var existingScheduledPayment = await _dbContext.ScheduledPayments
                    .FirstOrDefaultAsync(sp => sp.IdempotencyKey == request.IdempotencyKey);

                if (existingScheduledPayment != null)
                {
                    _logger.LogInformation("Scheduled payment with idempotency key {IdempotencyKey} already exists", request.IdempotencyKey);
                    return existingScheduledPayment;
                }

                var scheduledPayment = new ScheduledPayment
                {
                    TenantId = _tenantService.GetCurrentTenantId(),
                    Amount = request.Amount,
                    Currency = request.Currency,
                    PaymentMethod = request.PaymentMethod,
                    ScheduledDate = request.ScheduledDate,
                    Status = "Scheduled",
                    Description = request.Description,
                    IdempotencyKey = request.IdempotencyKey,
                    CreatedAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };

                await _dbContext.ScheduledPayments.AddAsync(scheduledPayment);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Payment {ScheduledPaymentId} scheduled for {ScheduledDate}", scheduledPayment.Id, request.ScheduledDate);
                return scheduledPayment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling payment");
                throw;
            }
        }

        public async Task<Payment> ProcessSmsUsageBillingAsync(SmsUsageBillingRequest request)
        {
            try
            {
                _logger.LogInformation("Processing SMS usage billing for tenant {TenantId}, messages: {MessageCount}", 
                    _tenantService.GetCurrentTenantId(), request.MessageCount);

                var smsRates = await GetSmsRatesAsync(request.Country, request.MessageType);
                var totalCost = request.MessageCount * smsRates.CostPerMessage;

                if (request.Currency != smsRates.Currency)
                {
                    totalCost = await ConvertCurrencyAsync(totalCost, smsRates.Currency, request.Currency);
                }

                var transaction = new Transaction
                {
                    TenantId = _tenantService.GetCurrentTenantId(),
                    Amount = totalCost,
                    Currency = request.Currency,
                    Type = "SMS_Usage",
                    Status = "Pending",
                    Description = $"SMS usage billing - {request.MessageCount} messages to {request.Country}",
                    CreatedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        { "messageCount", request.MessageCount },
                        { "country", request.Country },
                        { "messageType", request.MessageType },
                        { "costPerMessage", smsRates.CostPerMessage },
                        { "originalCurrency", smsRates.Currency }
                    }
                };

                await _dbContext.Transactions.AddAsync(transaction);
                await _dbContext.SaveChangesAsync();

                var paymentRequest = new PaymentRequest
                {
                    TransactionId = transaction.Id,
                    PaymentMethod = request.PaymentMethod,
                    Amount = totalCost,
                    Currency = request.Currency,
                    IdempotencyKey = request.IdempotencyKey
                };

                var payment = await ProcessPaymentAsync(paymentRequest);
                
                _logger.LogInformation("SMS usage billing processed. Cost: {Cost} {Currency} for {MessageCount} messages", 
                    totalCost, request.Currency, request.MessageCount);

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SMS usage billing");
                throw;
            }
        }

        public async Task<List<Payment>> GetPaymentHistoryAsync(int tenantId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _dbContext.Payments
                    .Where(p => p.TenantId == tenantId);

                if (startDate.HasValue)
                    query = query.Where(p => p.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(p => p.CreatedAt <= endDate.Value);

                return await query
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment history for tenant {TenantId}", tenantId);
                throw;
            }
        }

        private DateTime CalculateNextBillingDate(DateTime currentDate, string billingCycle)
        {
            return billingCycle.ToLower() switch
            {
                "monthly" => currentDate.AddMonths(1),
                "quarterly" => currentDate.AddMonths(3),
                "yearly" => currentDate.AddYears(1),
                "weekly" => currentDate.AddDays(7),
                _ => currentDate.AddMonths(1)
            };
        }

        private async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            await Task.Delay(50);

            var rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["USD"] = new Dictionary<string, decimal>
                {
                    ["EUR"] = 0.85m,
                    ["GBP"] = 0.73m,
                    ["SAR"] = 3.75m,
                    ["AED"] = 3.67m,
                    ["JPY"] = 110.0m
                },
                ["EUR"] = new Dictionary<string, decimal>
                {
                    ["USD"] = 1.18m,
                    ["GBP"] = 0.86m,
                    ["SAR"] = 4.41m,
                    ["AED"] = 4.33m,
                    ["JPY"] = 129.4m
                },
                ["SAR"] = new Dictionary<string, decimal>
                {
                    ["USD"] = 0.27m,
                    ["EUR"] = 0.23m,
                    ["GBP"] = 0.19m,
                    ["AED"] = 0.98m,
                    ["JPY"] = 29.3m
                }
            };

            if (rates.ContainsKey(fromCurrency) && rates[fromCurrency].ContainsKey(toCurrency))
            {
                return rates[fromCurrency][toCurrency];
            }

            return 1.0m;
        }

        private async Task<SmsRates> GetSmsRatesAsync(string country, string messageType)
        {
            await Task.Delay(50);

            var rates = new Dictionary<string, SmsRates>
            {
                ["SA"] = new SmsRates { Country = "SA", CostPerMessage = 0.05m, Currency = "SAR" },
                ["US"] = new SmsRates { Country = "US", CostPerMessage = 0.01m, Currency = "USD" },
                ["GB"] = new SmsRates { Country = "GB", CostPerMessage = 0.02m, Currency = "GBP" },
                ["AE"] = new SmsRates { Country = "AE", CostPerMessage = 0.04m, Currency = "AED" },
                ["DE"] = new SmsRates { Country = "DE", CostPerMessage = 0.03m, Currency = "EUR" }
            };

            if (rates.ContainsKey(country))
            {
                var rate = rates[country];
                if (messageType == "premium")
                    rate.CostPerMessage *= 1.5m;
                return rate;
            }

            return new SmsRates { Country = country, CostPerMessage = 0.05m, Currency = "USD" };
        }

        private string ExtractCountryFromRequest(PaymentRequest request)
        {
            try
            {
                if (!string.IsNullOrEmpty(request.Metadata))
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Metadata);
                    if (metadata?.ContainsKey("country") == true)
                    {
                        return metadata["country"]?.ToString() ?? "SA";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract country from payment request metadata");
            }
            
            return "SA"; // Default to Saudi Arabia
        }

        private string ExtractIpFromRequest(PaymentRequest request)
        {
            try
            {
                if (!string.IsNullOrEmpty(request.Metadata))
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Metadata);
                    if (metadata?.ContainsKey("ipAddress") == true)
                    {
                        return metadata["ipAddress"]?.ToString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract IP address from payment request metadata");
            }
            
            return string.Empty;
        }

        private async Task<bool> IsNewCardAsync(string paymentMethod, string externalReference)
        {
            try
            {
                if (paymentMethod?.ToLower() != "creditcard" && paymentMethod?.ToLower() != "debitcard")
                    return false;

                if (string.IsNullOrEmpty(externalReference))
                    return true;

                var existingPayment = await _dbContext.Payments
                    .Where(p => p.ExternalReference == externalReference && p.Status == "Completed")
                    .FirstOrDefaultAsync();

                return existingPayment == null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check if card is new, defaulting to true");
                return true;
            }
        }
    }

    public class PaymentRequest
    {
        public int TransactionId { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public class RefundRequest
    {
        public int PaymentId { get; set; }
        public decimal? Amount { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string ReferenceId { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class Subscription
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string PlanName { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string BillingCycle { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime NextBillingDate { get; set; }
        public string PaymentMethod { get; set; }
        public string IdempotencyKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class ScheduledPayment
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string IdempotencyKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class CreateSubscriptionRequest
    {
        public string PlanName { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string BillingCycle { get; set; }
        public DateTime? StartDate { get; set; }
        public string PaymentMethod { get; set; }
        public string IdempotencyKey { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class SchedulePaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Description { get; set; }
        public string IdempotencyKey { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class SmsUsageBillingRequest
    {
        public int MessageCount { get; set; }
        public string Country { get; set; }
        public string MessageType { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public class SmsRates
    {
        public string Country { get; set; }
        public decimal CostPerMessage { get; set; }
        public string Currency { get; set; }
    }

    public interface IPaymentService
    {
        Task<Payment> ProcessPaymentAsync(PaymentRequest request);
        Task<Payment> RefundPaymentAsync(RefundRequest request);
        Task<Subscription> CreateSubscriptionAsync(CreateSubscriptionRequest request);
        Task<Payment> ProcessRecurringPaymentAsync(int subscriptionId);
        Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency);
        Task<ScheduledPayment> SchedulePaymentAsync(SchedulePaymentRequest request);
        Task<Payment> ProcessSmsUsageBillingAsync(SmsUsageBillingRequest request);
        Task<List<Payment>> GetPaymentHistoryAsync(int tenantId, DateTime? startDate = null, DateTime? endDate = null);
    }
}

