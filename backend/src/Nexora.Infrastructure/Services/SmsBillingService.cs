using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexora.Core.Data;
using Nexora.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Services
{
    public class SmsBillingService : ISmsBillingService
    {
        private readonly NexoraDbContext _dbContext;
        private readonly ILogger<SmsBillingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITenantService _tenantService;
        private readonly IPaymentService _paymentService;
        private readonly ICacheService _cacheService;

        public SmsBillingService(
            NexoraDbContext dbContext,
            ILogger<SmsBillingService> logger,
            IConfiguration configuration,
            ITenantService tenantService,
            IPaymentService paymentService,
            ICacheService cacheService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _configuration = configuration;
            _tenantService = tenantService;
            _paymentService = paymentService;
            _cacheService = cacheService;
        }

        public async Task<SmsBillingResult> CalculateUsageCostAsync(SmsUsageRequest request)
        {
            try
            {
                _logger.LogInformation("Calculating SMS usage cost for tenant {TenantId}, messages: {MessageCount}", 
                    request.TenantId, request.MessageCount);

                var rateCard = await GetRateForCountryAsync(request.Country, request.MessageType);
                var costPerMessage = rateCard.StandardRate;

                if (request.MessageType?.ToLower() == "premium")
                {
                    costPerMessage = rateCard.PremiumRate;
                }

                var totalCost = request.MessageCount * costPerMessage;

                if (request.Currency != rateCard.Currency)
                {
                    totalCost = await _paymentService.ConvertCurrencyAsync(totalCost, rateCard.Currency, request.Currency);
                }

                var billingCycle = await GetOrCreateCurrentBillingCycleAsync(request.TenantId, request.Currency);

                var smsTransaction = new SmsTransaction
                {
                    TenantId = request.TenantId,
                    TransactionType = "Usage",
                    MessageCount = request.MessageCount,
                    Amount = totalCost,
                    Currency = request.Currency,
                    Country = request.Country,
                    MessageType = request.MessageType,
                    TransactionDate = request.UsageDate,
                    Status = "Completed",
                    Metadata = request.Metadata ?? new Dictionary<string, object>()
                };

                await RecordTransactionAsync(smsTransaction);

                await UpdateBillingCycleUsageAsync(billingCycle.Id, request.MessageCount, totalCost);

                var result = new SmsBillingResult
                {
                    Success = true,
                    TotalCost = totalCost,
                    Currency = request.Currency,
                    MessageCount = request.MessageCount,
                    CostPerMessage = totalCost / request.MessageCount,
                    BillingCycleId = billingCycle.Id.ToString(),
                    TransactionId = smsTransaction.Id.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        { "country", request.Country },
                        { "messageType", request.MessageType },
                        { "originalRate", costPerMessage },
                        { "originalCurrency", rateCard.Currency }
                    }
                };

                _logger.LogInformation("SMS usage cost calculated: {Cost} {Currency} for {MessageCount} messages", 
                    totalCost, request.Currency, request.MessageCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating SMS usage cost for tenant {TenantId}", request.TenantId);
                return new SmsBillingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<SmsBillingCycle> CreateBillingCycleAsync(CreateBillingCycleRequest request)
        {
            try
            {
                _logger.LogInformation("Creating billing cycle for tenant {TenantId}", request.TenantId);

                var existingCycle = await _dbContext.SmsBillingCycles
                    .FirstOrDefaultAsync(bc => bc.TenantId == request.TenantId && 
                                              bc.StartDate <= request.EndDate && 
                                              bc.EndDate >= request.StartDate &&
                                              bc.Status != "Closed");

                if (existingCycle != null)
                {
                    _logger.LogWarning("Overlapping billing cycle found for tenant {TenantId}", request.TenantId);
                    return existingCycle;
                }

                var billingCycle = new SmsBillingCycle
                {
                    TenantId = request.TenantId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Status = "Active",
                    TotalCost = 0,
                    Currency = request.Currency,
                    TotalMessages = 0,
                    CreatedAt = DateTime.UtcNow,
                    Metadata = request.Metadata ?? new Dictionary<string, object>()
                };

                await _dbContext.SmsBillingCycles.AddAsync(billingCycle);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Billing cycle {BillingCycleId} created for tenant {TenantId}", 
                    billingCycle.Id, request.TenantId);

                return billingCycle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating billing cycle for tenant {TenantId}", request.TenantId);
                throw;
            }
        }

        public async Task<SmsBillingCycle> GetCurrentBillingCycleAsync(int tenantId)
        {
            try
            {
                var currentDate = DateTime.UtcNow;
                var billingCycle = await _dbContext.SmsBillingCycles
                    .FirstOrDefaultAsync(bc => bc.TenantId == tenantId && 
                                              bc.StartDate <= currentDate && 
                                              bc.EndDate >= currentDate &&
                                              bc.Status == "Active");

                if (billingCycle == null)
                {
                    var settings = await GetBillingSettingsAsync(tenantId);
                    var startDate = GetBillingCycleStartDate(settings.BillingCycle);
                    var endDate = GetBillingCycleEndDate(startDate, settings.BillingCycle);

                    var createRequest = new CreateBillingCycleRequest
                    {
                        TenantId = tenantId,
                        StartDate = startDate,
                        EndDate = endDate,
                        Currency = settings.Currency
                    };

                    billingCycle = await CreateBillingCycleAsync(createRequest);
                }

                return billingCycle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current billing cycle for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<List<SmsBillingCycle>> GetBillingHistoryAsync(int tenantId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _dbContext.SmsBillingCycles
                    .Where(bc => bc.TenantId == tenantId);

                if (startDate.HasValue)
                    query = query.Where(bc => bc.StartDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(bc => bc.EndDate <= endDate.Value);

                return await query
                    .OrderByDescending(bc => bc.StartDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing history for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<SmsUsageSummary> GetUsageSummaryAsync(int tenantId, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Getting SMS usage summary for tenant {TenantId} from {StartDate} to {EndDate}", 
                    tenantId, startDate, endDate);

                var transactions = await _dbContext.SmsTransactions
                    .Where(t => t.TenantId == tenantId && 
                               t.TransactionDate >= startDate && 
                               t.TransactionDate <= endDate &&
                               t.TransactionType == "Usage")
                    .ToListAsync();

                var totalMessages = transactions.Sum(t => t.MessageCount);
                var totalCost = transactions.Sum(t => t.Amount);
                var currency = transactions.FirstOrDefault()?.Currency ?? "USD";

                var usageByCountry = transactions
                    .GroupBy(t => t.Country)
                    .ToDictionary(g => g.Key, g => new SmsCountryUsage
                    {
                        Country = g.Key,
                        MessageCount = g.Sum(t => t.MessageCount),
                        Cost = g.Sum(t => t.Amount),
                        CostPerMessage = g.Sum(t => t.Amount) / g.Sum(t => t.MessageCount)
                    });

                var usageByType = transactions
                    .GroupBy(t => t.MessageType)
                    .ToDictionary(g => g.Key, g => new SmsTypeUsage
                    {
                        MessageType = g.Key,
                        MessageCount = g.Sum(t => t.MessageCount),
                        Cost = g.Sum(t => t.Amount),
                        CostPerMessage = g.Sum(t => t.Amount) / g.Sum(t => t.MessageCount)
                    });

                var dailyUsage = transactions
                    .GroupBy(t => t.TransactionDate.Date)
                    .Select(g => new SmsDailyUsage
                    {
                        Date = g.Key,
                        MessageCount = g.Sum(t => t.MessageCount),
                        Cost = g.Sum(t => t.Amount)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                return new SmsUsageSummary
                {
                    TenantId = tenantId,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalMessages = totalMessages,
                    TotalCost = totalCost,
                    Currency = currency,
                    UsageByCountry = usageByCountry,
                    UsageByType = usageByType,
                    DailyUsage = dailyUsage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage summary for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<SmsBillingResult> ProcessBillingCycleAsync(int billingCycleId)
        {
            try
            {
                _logger.LogInformation("Processing billing cycle {BillingCycleId}", billingCycleId);

                var billingCycle = await _dbContext.SmsBillingCycles.FindAsync(billingCycleId);
                if (billingCycle == null)
                {
                    throw new ArgumentException($"Billing cycle {billingCycleId} not found");
                }

                if (billingCycle.Status != "Active")
                {
                    throw new InvalidOperationException($"Cannot process billing cycle with status {billingCycle.Status}");
                }

                var settings = await GetBillingSettingsAsync(billingCycle.TenantId);

                if (billingCycle.TotalCost > 0)
                {
                    var billingRequest = new SmsUsageBillingRequest
                    {
                        MessageCount = billingCycle.TotalMessages,
                        Country = "BILLING_CYCLE",
                        MessageType = "billing",
                        Currency = billingCycle.Currency,
                        PaymentMethod = settings.PaymentMethod,
                        IdempotencyKey = $"billing_cycle_{billingCycleId}_{DateTime.UtcNow:yyyyMMdd}"
                    };

                    var payment = await _paymentService.ProcessSmsUsageBillingAsync(billingRequest);

                    if (payment.Status == "Completed")
                    {
                        billingCycle.Status = "Processed";
                        billingCycle.ProcessedAt = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync();

                        _logger.LogInformation("Billing cycle {BillingCycleId} processed successfully. Amount: {Amount} {Currency}", 
                            billingCycleId, billingCycle.TotalCost, billingCycle.Currency);
                    }
                    else
                    {
                        billingCycle.Status = "Failed";
                        await _dbContext.SaveChangesAsync();

                        _logger.LogError("Failed to process payment for billing cycle {BillingCycleId}", billingCycleId);
                    }
                }
                else
                {
                    billingCycle.Status = "Processed";
                    billingCycle.ProcessedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                return new SmsBillingResult
                {
                    Success = billingCycle.Status == "Processed",
                    TotalCost = billingCycle.TotalCost,
                    Currency = billingCycle.Currency,
                    MessageCount = billingCycle.TotalMessages,
                    BillingCycleId = billingCycleId.ToString(),
                    ErrorMessage = billingCycle.Status == "Failed" ? "Payment processing failed" : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing billing cycle {BillingCycleId}", billingCycleId);
                throw;
            }
        }

        public async Task<List<SmsRateCard>> GetRateCardAsync(string country = null)
        {
            try
            {
                var cacheKey = $"sms_rate_card_{country ?? "all"}";
                var cachedRates = await _cacheService.GetAsync<List<SmsRateCard>>(cacheKey);

                if (cachedRates != null)
                {
                    return cachedRates;
                }

                var rates = GetDefaultRateCard();

                if (!string.IsNullOrEmpty(country))
                {
                    rates = rates.Where(r => r.Country.Equals(country, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                await _cacheService.SetAsync(cacheKey, rates, TimeSpan.FromHours(1));
                return rates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rate card for country {Country}", country);
                throw;
            }
        }

        public async Task<SmsCostEstimate> EstimateCostAsync(SmsEstimateRequest request)
        {
            try
            {
                var rateCard = await GetRateForCountryAsync(request.Country, request.MessageType);
                var costPerMessage = request.MessageType?.ToLower() == "premium" ? rateCard.PremiumRate : rateCard.StandardRate;
                var totalCost = request.MessageCount * costPerMessage;

                if (request.Currency != rateCard.Currency)
                {
                    totalCost = await _paymentService.ConvertCurrencyAsync(totalCost, rateCard.Currency, request.Currency);
                }

                return new SmsCostEstimate
                {
                    MessageCount = request.MessageCount,
                    EstimatedCost = totalCost,
                    Currency = request.Currency,
                    CostPerMessage = totalCost / request.MessageCount,
                    Country = request.Country,
                    MessageType = request.MessageType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating SMS cost");
                throw;
            }
        }

        public async Task<SmsBillingAlert> CheckBillingAlertsAsync(int tenantId)
        {
            try
            {
                var settings = await GetBillingSettingsAsync(tenantId);
                if (!settings.EnableAlerts)
                {
                    return null;
                }

                var currentCycle = await GetCurrentBillingCycleAsync(tenantId);
                var currentUsage = currentCycle.TotalCost;

                if (currentUsage >= settings.AlertThreshold)
                {
                    return new SmsBillingAlert
                    {
                        TenantId = tenantId,
                        AlertType = currentUsage >= settings.CreditLimit ? "CREDIT_LIMIT_EXCEEDED" : "THRESHOLD_EXCEEDED",
                        Message = currentUsage >= settings.CreditLimit 
                            ? $"Credit limit of {settings.CreditLimit} {settings.Currency} has been exceeded"
                            : $"Alert threshold of {settings.AlertThreshold} {settings.Currency} has been reached",
                        CurrentUsage = currentUsage,
                        Threshold = settings.AlertThreshold,
                        Currency = settings.Currency,
                        AlertDate = DateTime.UtcNow,
                        IsActive = true
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking billing alerts for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<SmsBillingResult> ApplyCreditsAsync(ApplyCreditsRequest request)
        {
            try
            {
                _logger.LogInformation("Applying credits for tenant {TenantId}: {Amount} {Currency}", 
                    request.TenantId, request.CreditAmount, request.Currency);

                var creditTransaction = new SmsTransaction
                {
                    TenantId = request.TenantId,
                    TransactionType = "Credit",
                    MessageCount = 0,
                    Amount = -request.CreditAmount,
                    Currency = request.Currency,
                    Country = "CREDIT",
                    MessageType = "credit",
                    TransactionDate = DateTime.UtcNow,
                    Status = "Completed",
                    Metadata = request.Metadata ?? new Dictionary<string, object>
                    {
                        { "reason", request.Reason },
                        { "referenceId", request.ReferenceId }
                    }
                };

                await RecordTransactionAsync(creditTransaction);

                var currentCycle = await GetCurrentBillingCycleAsync(request.TenantId);
                await UpdateBillingCycleUsageAsync(currentCycle.Id, 0, -request.CreditAmount);

                return new SmsBillingResult
                {
                    Success = true,
                    TotalCost = -request.CreditAmount,
                    Currency = request.Currency,
                    MessageCount = 0,
                    BillingCycleId = currentCycle.Id.ToString(),
                    TransactionId = creditTransaction.Id.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        { "creditApplied", true },
                        { "reason", request.Reason }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying credits for tenant {TenantId}", request.TenantId);
                throw;
            }
        }

        public async Task<List<SmsTransaction>> GetTransactionHistoryAsync(int tenantId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _dbContext.SmsTransactions
                    .Where(t => t.TenantId == tenantId);

                if (startDate.HasValue)
                    query = query.Where(t => t.TransactionDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(t => t.TransactionDate <= endDate.Value);

                return await query
                    .OrderByDescending(t => t.TransactionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction history for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<SmsBillingSettings> GetBillingSettingsAsync(int tenantId)
        {
            try
            {
                var settings = await _dbContext.SmsBillingSettings
                    .FirstOrDefaultAsync(s => s.TenantId == tenantId);

                if (settings == null)
                {
                    settings = new SmsBillingSettings
                    {
                        TenantId = tenantId,
                        BillingCycle = "monthly",
                        Currency = "USD",
                        CreditLimit = 1000,
                        AlertThreshold = 800,
                        AutoRecharge = false,
                        AutoRechargeAmount = 500,
                        PaymentMethod = "default",
                        EnableAlerts = true,
                        Metadata = new Dictionary<string, object>()
                    };

                    await _dbContext.SmsBillingSettings.AddAsync(settings);
                    await _dbContext.SaveChangesAsync();
                }

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing settings for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<SmsBillingSettings> UpdateBillingSettingsAsync(int tenantId, UpdateBillingSettingsRequest request)
        {
            try
            {
                var settings = await GetBillingSettingsAsync(tenantId);

                if (!string.IsNullOrEmpty(request.BillingCycle))
                    settings.BillingCycle = request.BillingCycle;

                if (!string.IsNullOrEmpty(request.Currency))
                    settings.Currency = request.Currency;

                if (request.CreditLimit.HasValue)
                    settings.CreditLimit = request.CreditLimit.Value;

                if (request.AlertThreshold.HasValue)
                    settings.AlertThreshold = request.AlertThreshold.Value;

                if (request.AutoRecharge.HasValue)
                    settings.AutoRecharge = request.AutoRecharge.Value;

                if (request.AutoRechargeAmount.HasValue)
                    settings.AutoRechargeAmount = request.AutoRechargeAmount.Value;

                if (!string.IsNullOrEmpty(request.PaymentMethod))
                    settings.PaymentMethod = request.PaymentMethod;

                if (request.EnableAlerts.HasValue)
                    settings.EnableAlerts = request.EnableAlerts.Value;

                if (request.Metadata != null)
                    settings.Metadata = request.Metadata;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Billing settings updated for tenant {TenantId}", tenantId);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating billing settings for tenant {TenantId}", tenantId);
                throw;
            }
        }

        private async Task<SmsRateCard> GetRateForCountryAsync(string country, string messageType)
        {
            var rates = await GetRateCardAsync(country);
            var rate = rates.FirstOrDefault(r => r.Country.Equals(country, StringComparison.OrdinalIgnoreCase));

            if (rate == null)
            {
                rate = new SmsRateCard
                {
                    Country = country,
                    CountryCode = country,
                    StandardRate = 0.05m,
                    PremiumRate = 0.08m,
                    Currency = "USD",
                    EffectiveDate = DateTime.UtcNow,
                    IsActive = true
                };
            }

            return rate;
        }

        private async Task<SmsBillingCycle> GetOrCreateCurrentBillingCycleAsync(int tenantId, string currency)
        {
            var currentCycle = await GetCurrentBillingCycleAsync(tenantId);
            
            if (currentCycle.Currency != currency)
            {
                var convertedCost = await _paymentService.ConvertCurrencyAsync(currentCycle.TotalCost, currentCycle.Currency, currency);
                currentCycle.TotalCost = convertedCost;
                currentCycle.Currency = currency;
                await _dbContext.SaveChangesAsync();
            }

            return currentCycle;
        }

        private async Task RecordTransactionAsync(SmsTransaction transaction)
        {
            await _dbContext.SmsTransactions.AddAsync(transaction);
            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateBillingCycleUsageAsync(int billingCycleId, int messageCount, decimal cost)
        {
            var billingCycle = await _dbContext.SmsBillingCycles.FindAsync(billingCycleId);
            if (billingCycle != null)
            {
                billingCycle.TotalMessages += messageCount;
                billingCycle.TotalCost += cost;
                await _dbContext.SaveChangesAsync();
            }
        }

        private List<SmsRateCard> GetDefaultRateCard()
        {
            return new List<SmsRateCard>
            {
                new SmsRateCard { Country = "SA", CountryCode = "966", StandardRate = 0.05m, PremiumRate = 0.08m, Currency = "SAR", EffectiveDate = DateTime.UtcNow, IsActive = true },
                new SmsRateCard { Country = "US", CountryCode = "1", StandardRate = 0.01m, PremiumRate = 0.02m, Currency = "USD", EffectiveDate = DateTime.UtcNow, IsActive = true },
                new SmsRateCard { Country = "GB", CountryCode = "44", StandardRate = 0.02m, PremiumRate = 0.04m, Currency = "GBP", EffectiveDate = DateTime.UtcNow, IsActive = true },
                new SmsRateCard { Country = "AE", CountryCode = "971", StandardRate = 0.04m, PremiumRate = 0.06m, Currency = "AED", EffectiveDate = DateTime.UtcNow, IsActive = true },
                new SmsRateCard { Country = "DE", CountryCode = "49", StandardRate = 0.03m, PremiumRate = 0.05m, Currency = "EUR", EffectiveDate = DateTime.UtcNow, IsActive = true },
                new SmsRateCard { Country = "FR", CountryCode = "33", StandardRate = 0.03m, PremiumRate = 0.05m, Currency = "EUR", EffectiveDate = DateTime.UtcNow, IsActive = true },
                new SmsRateCard { Country = "IN", CountryCode = "91", StandardRate = 0.008m, PremiumRate = 0.015m, Currency = "USD", EffectiveDate = DateTime.UtcNow, IsActive = true },
                new SmsRateCard { Country = "AU", CountryCode = "61", StandardRate = 0.04m, PremiumRate = 0.07m, Currency = "USD", EffectiveDate = DateTime.UtcNow, IsActive = true },
                new SmsRateCard { Country = "CA", CountryCode = "1", StandardRate = 0.01m, PremiumRate = 0.02m, Currency = "USD", EffectiveDate = DateTime.UtcNow, IsActive = true },
                new SmsRateCard { Country = "JP", CountryCode = "81", StandardRate = 0.06m, PremiumRate = 0.10m, Currency = "USD", EffectiveDate = DateTime.UtcNow, IsActive = true }
            };
        }

        private DateTime GetBillingCycleStartDate(string billingCycle)
        {
            var now = DateTime.UtcNow;
            return billingCycle.ToLower() switch
            {
                "weekly" => now.AddDays(-(int)now.DayOfWeek).Date,
                "monthly" => new DateTime(now.Year, now.Month, 1),
                "quarterly" => new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1),
                "yearly" => new DateTime(now.Year, 1, 1),
                _ => new DateTime(now.Year, now.Month, 1)
            };
        }

        private DateTime GetBillingCycleEndDate(DateTime startDate, string billingCycle)
        {
            return billingCycle.ToLower() switch
            {
                "weekly" => startDate.AddDays(7).AddTicks(-1),
                "monthly" => startDate.AddMonths(1).AddTicks(-1),
                "quarterly" => startDate.AddMonths(3).AddTicks(-1),
                "yearly" => startDate.AddYears(1).AddTicks(-1),
                _ => startDate.AddMonths(1).AddTicks(-1)
            };
        }
    }
}
