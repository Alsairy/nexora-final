using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using System.Text.Json;

namespace Nexora.Core.AI;

public interface IFraudDetectionService
{
    Task<FraudRiskAnalysis> AnalyzeTransactionAsync(Transaction transaction);
    Task<FraudRiskAnalysis> AnalyzePaymentAsync(Payment payment);
    Task<SpendingAnalysis> AnalyzeSpendingPatternsAsync(string userId, DateTime fromDate, DateTime toDate);
    Task<PersonalizedInsights> GenerateInsightsAsync(string userId);
    Task UpdateFraudModelAsync(FraudFeedback feedback);
}

public class FraudDetectionService : IFraudDetectionService
{
    private readonly ILogger<FraudDetectionService> _logger;
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IConfiguration _configuration;

    public FraudDetectionService(
        ILogger<FraudDetectionService> logger,
        IRepository<Transaction> transactionRepository,
        IRepository<Payment> paymentRepository,
        IRepository<User> userRepository,
        IConfiguration configuration)
    {
        _logger = logger;
        _transactionRepository = transactionRepository;
        _paymentRepository = paymentRepository;
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<FraudRiskAnalysis> AnalyzeTransactionAsync(Transaction transaction)
    {
        try
        {
            var features = await ExtractTransactionFeaturesAsync(transaction);
            var riskScore = CalculateRiskScore(features);
            var riskLevel = DetermineRiskLevel(riskScore);
            
            var analysis = new FraudRiskAnalysis
            {
                TransactionId = transaction.Id,
                RiskScore = riskScore,
                RiskLevel = riskLevel,
                Factors = features.RiskFactors,
                Recommendations = GenerateRecommendations(riskLevel, features),
                AnalyzedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Fraud analysis completed for transaction {TransactionId}: Risk={RiskLevel}, Score={RiskScore}", 
                transaction.Id, riskLevel, riskScore);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing transaction {TransactionId} for fraud", transaction.Id);
            return new FraudRiskAnalysis
            {
                TransactionId = transaction.Id,
                RiskScore = 0.5,
                RiskLevel = FraudRiskLevel.Medium,
                Factors = new List<string> { "Analysis failed - manual review required" },
                AnalyzedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<FraudRiskAnalysis> AnalyzePaymentAsync(Payment payment)
    {
        try
        {
            var features = await ExtractPaymentFeaturesAsync(payment);
            var riskScore = CalculateRiskScore(features);
            var riskLevel = DetermineRiskLevel(riskScore);
            
            var analysis = new FraudRiskAnalysis
            {
                PaymentId = payment.Id,
                RiskScore = riskScore,
                RiskLevel = riskLevel,
                Factors = features.RiskFactors,
                Recommendations = GenerateRecommendations(riskLevel, features),
                AnalyzedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Fraud analysis completed for payment {PaymentId}: Risk={RiskLevel}, Score={RiskScore}", 
                payment.Id, riskLevel, riskScore);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing payment {PaymentId} for fraud", payment.Id);
            return new FraudRiskAnalysis
            {
                PaymentId = payment.Id,
                RiskScore = 0.5,
                RiskLevel = FraudRiskLevel.Medium,
                Factors = new List<string> { "Analysis failed - manual review required" },
                AnalyzedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<SpendingAnalysis> AnalyzeSpendingPatternsAsync(string userId, DateTime fromDate, DateTime toDate)
    {
        var transactions = await _transactionRepository.GetAsync(t => 
            t.UserId == userId && 
            t.CreatedAt >= fromDate && 
            t.CreatedAt <= toDate);

        var analysis = new SpendingAnalysis
        {
            UserId = userId,
            Period = new DateRange { From = fromDate, To = toDate },
            TotalSpent = transactions.Sum(t => t.Amount),
            TransactionCount = transactions.Count(),
            AverageTransaction = transactions.Any() ? transactions.Average(t => t.Amount) : 0,
            Categories = AnalyzeSpendingCategories(transactions),
            Trends = AnalyzeSpendingTrends(transactions),
            Anomalies = DetectSpendingAnomalies(transactions),
            AnalyzedAt = DateTime.UtcNow
        };

        return analysis;
    }

    public async Task<PersonalizedInsights> GenerateInsightsAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        var recentTransactions = await _transactionRepository.GetAsync(t => 
            t.UserId == userId && 
            t.CreatedAt >= DateTime.UtcNow.AddDays(-30));

        var insights = new PersonalizedInsights
        {
            UserId = userId,
            BudgetRecommendations = GenerateBudgetRecommendations(recentTransactions),
            SavingsOpportunities = IdentifySavingsOpportunities(recentTransactions),
            SpendingAlerts = GenerateSpendingAlerts(recentTransactions),
            SecurityRecommendations = GenerateSecurityRecommendations(user, recentTransactions),
            GeneratedAt = DateTime.UtcNow
        };

        return insights;
    }

    public async Task UpdateFraudModelAsync(FraudFeedback feedback)
    {
        _logger.LogInformation("Updating fraud model with feedback: {FeedbackType} for {EntityType}:{EntityId}", 
            feedback.FeedbackType, feedback.EntityType, feedback.EntityId);
        
        await Task.CompletedTask;
    }

    private async Task<FraudFeatures> ExtractTransactionFeaturesAsync(Transaction transaction)
    {
        var features = new FraudFeatures();
        var userTransactions = await _transactionRepository.GetAsync(t => 
            t.UserId == transaction.UserId && 
            t.CreatedAt >= DateTime.UtcNow.AddDays(-30));

        features.Amount = transaction.Amount;
        features.IsHighValue = transaction.Amount > 10000;
        features.IsUnusualAmount = IsUnusualAmount(transaction.Amount, userTransactions);
        features.IsOffHours = IsOffHours(transaction.CreatedAt);
        features.VelocityScore = CalculateVelocityScore(transaction, userTransactions);
        features.LocationRisk = CalculateLocationRisk(transaction.Metadata);
        features.DeviceRisk = CalculateDeviceRisk(transaction.Metadata);
        
        features.RiskFactors = new List<string>();
        if (features.IsHighValue) features.RiskFactors.Add("High value transaction");
        if (features.IsUnusualAmount) features.RiskFactors.Add("Unusual amount for user");
        if (features.IsOffHours) features.RiskFactors.Add("Transaction outside normal hours");
        if (features.VelocityScore > 0.7) features.RiskFactors.Add("High transaction velocity");
        if (features.LocationRisk > 0.6) features.RiskFactors.Add("Suspicious location");
        if (features.DeviceRisk > 0.6) features.RiskFactors.Add("Unrecognized device");

        return features;
    }

    private async Task<FraudFeatures> ExtractPaymentFeaturesAsync(Payment payment)
    {
        var features = new FraudFeatures();
        var userPayments = await _paymentRepository.GetAsync(p => 
            p.UserId == payment.UserId && 
            p.CreatedAt >= DateTime.UtcNow.AddDays(-30));

        features.Amount = payment.Amount;
        features.IsHighValue = payment.Amount > 5000;
        features.IsUnusualAmount = IsUnusualPaymentAmount(payment.Amount, userPayments);
        features.IsOffHours = IsOffHours(payment.CreatedAt);
        features.VelocityScore = CalculatePaymentVelocityScore(payment, userPayments);
        features.ProviderRisk = CalculateProviderRisk(payment.Provider);
        
        features.RiskFactors = new List<string>();
        if (features.IsHighValue) features.RiskFactors.Add("High value payment");
        if (features.IsUnusualAmount) features.RiskFactors.Add("Unusual payment amount for user");
        if (features.IsOffHours) features.RiskFactors.Add("Payment outside normal hours");
        if (features.VelocityScore > 0.7) features.RiskFactors.Add("High payment velocity");
        if (features.ProviderRisk > 0.5) features.RiskFactors.Add("High-risk payment provider");

        return features;
    }

    private double CalculateRiskScore(FraudFeatures features)
    {
        double score = 0.0;
        
        if (features.IsHighValue) score += 0.3;
        if (features.IsUnusualAmount) score += 0.25;
        if (features.IsOffHours) score += 0.15;
        
        score += features.VelocityScore * 0.2;
        score += features.LocationRisk * 0.15;
        score += features.DeviceRisk * 0.1;
        score += features.ProviderRisk * 0.1;
        
        return Math.Min(1.0, score);
    }

    private FraudRiskLevel DetermineRiskLevel(double riskScore)
    {
        return riskScore switch
        {
            >= 0.8 => FraudRiskLevel.Critical,
            >= 0.6 => FraudRiskLevel.High,
            >= 0.4 => FraudRiskLevel.Medium,
            >= 0.2 => FraudRiskLevel.Low,
            _ => FraudRiskLevel.Minimal
        };
    }

    private List<string> GenerateRecommendations(FraudRiskLevel riskLevel, FraudFeatures features)
    {
        var recommendations = new List<string>();
        
        switch (riskLevel)
        {
            case FraudRiskLevel.Critical:
                recommendations.Add("Block transaction immediately");
                recommendations.Add("Require manual review");
                recommendations.Add("Contact user for verification");
                break;
            case FraudRiskLevel.High:
                recommendations.Add("Require additional authentication");
                recommendations.Add("Flag for manual review");
                break;
            case FraudRiskLevel.Medium:
                recommendations.Add("Monitor closely");
                recommendations.Add("Consider additional verification");
                break;
            case FraudRiskLevel.Low:
                recommendations.Add("Standard processing");
                break;
        }
        
        return recommendations;
    }

    private bool IsUnusualAmount(decimal amount, IEnumerable<Transaction> userTransactions)
    {
        if (!userTransactions.Any()) return amount > 1000;
        
        var avgAmount = userTransactions.Average(t => t.Amount);
        var stdDev = CalculateStandardDeviation(userTransactions.Select(t => (double)t.Amount));
        
        return Math.Abs((double)amount - (double)avgAmount) > (2 * stdDev);
    }

    private bool IsUnusualPaymentAmount(decimal amount, IEnumerable<Payment> userPayments)
    {
        if (!userPayments.Any()) return amount > 500;
        
        var avgAmount = userPayments.Average(p => p.Amount);
        var stdDev = CalculateStandardDeviation(userPayments.Select(p => (double)p.Amount));
        
        return Math.Abs((double)amount - (double)avgAmount) > (2 * stdDev);
    }

    private bool IsOffHours(DateTime timestamp)
    {
        var hour = timestamp.Hour;
        return hour < 6 || hour > 22;
    }

    private double CalculateVelocityScore(Transaction transaction, IEnumerable<Transaction> recentTransactions)
    {
        var last24Hours = recentTransactions.Where(t => t.CreatedAt >= DateTime.UtcNow.AddHours(-24));
        var transactionCount = last24Hours.Count();
        var totalAmount = last24Hours.Sum(t => t.Amount);
        
        var velocityScore = Math.Min(1.0, (transactionCount / 50.0) + ((double)totalAmount / 100000.0));
        return velocityScore;
    }

    private double CalculatePaymentVelocityScore(Payment payment, IEnumerable<Payment> recentPayments)
    {
        var last24Hours = recentPayments.Where(p => p.CreatedAt >= DateTime.UtcNow.AddHours(-24));
        var paymentCount = last24Hours.Count();
        var totalAmount = last24Hours.Sum(p => p.Amount);
        
        var velocityScore = Math.Min(1.0, (paymentCount / 20.0) + ((double)totalAmount / 50000.0));
        return velocityScore;
    }

    private double CalculateLocationRisk(string metadata)
    {
        return 0.1;
    }

    private double CalculateDeviceRisk(string metadata)
    {
        return 0.1;
    }

    private double CalculateProviderRisk(string provider)
    {
        var highRiskProviders = new[] { "unknown", "test", "demo" };
        return highRiskProviders.Contains(provider?.ToLower()) ? 0.8 : 0.1;
    }

    private double CalculateStandardDeviation(IEnumerable<double> values)
    {
        if (!values.Any()) return 0;
        
        var avg = values.Average();
        var sumOfSquares = values.Sum(x => Math.Pow(x - avg, 2));
        return Math.Sqrt(sumOfSquares / values.Count());
    }

    private Dictionary<string, decimal> AnalyzeSpendingCategories(IEnumerable<Transaction> transactions)
    {
        return transactions
            .GroupBy(t => t.Category ?? "Other")
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
    }

    private List<SpendingTrend> AnalyzeSpendingTrends(IEnumerable<Transaction> transactions)
    {
        return transactions
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new SpendingTrend
            {
                Date = g.Key,
                Amount = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(t => t.Date)
            .ToList();
    }

    private List<SpendingAnomaly> DetectSpendingAnomalies(IEnumerable<Transaction> transactions)
    {
        var anomalies = new List<SpendingAnomaly>();
        var amounts = transactions.Select(t => (double)t.Amount).ToList();
        
        if (amounts.Any())
        {
            var avg = amounts.Average();
            var stdDev = CalculateStandardDeviation(amounts);
            
            foreach (var transaction in transactions)
            {
                if (Math.Abs((double)transaction.Amount - avg) > (3 * stdDev))
                {
                    anomalies.Add(new SpendingAnomaly
                    {
                        TransactionId = transaction.Id,
                        Amount = transaction.Amount,
                        AnomalyType = "Unusual Amount",
                        Severity = transaction.Amount > (decimal)avg ? "High Spending" : "Low Spending",
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }
        }
        
        return anomalies;
    }

    private List<string> GenerateBudgetRecommendations(IEnumerable<Transaction> transactions)
    {
        var recommendations = new List<string>();
        var totalSpent = transactions.Sum(t => t.Amount);
        
        if (totalSpent > 5000)
        {
            recommendations.Add("Consider setting a monthly spending limit");
        }
        
        var categories = AnalyzeSpendingCategories(transactions);
        var topCategory = categories.OrderByDescending(c => c.Value).FirstOrDefault();
        
        if (topCategory.Value > totalSpent * 0.4m)
        {
            recommendations.Add($"High spending in {topCategory.Key} - consider budgeting");
        }
        
        return recommendations;
    }

    private List<string> IdentifySavingsOpportunities(IEnumerable<Transaction> transactions)
    {
        var opportunities = new List<string>();
        var categories = AnalyzeSpendingCategories(transactions);
        
        if (categories.ContainsKey("Dining") && categories["Dining"] > 1000)
        {
            opportunities.Add("Reduce dining expenses by cooking more at home");
        }
        
        if (categories.ContainsKey("Entertainment") && categories["Entertainment"] > 500)
        {
            opportunities.Add("Look for free entertainment alternatives");
        }
        
        return opportunities;
    }

    private List<string> GenerateSpendingAlerts(IEnumerable<Transaction> transactions)
    {
        var alerts = new List<string>();
        var recentTransactions = transactions.Where(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-7));
        var weeklySpending = recentTransactions.Sum(t => t.Amount);
        
        if (weeklySpending > 2000)
        {
            alerts.Add("High spending this week - review your transactions");
        }
        
        return alerts;
    }

    private List<string> GenerateSecurityRecommendations(User user, IEnumerable<Transaction> transactions)
    {
        var recommendations = new List<string>();
        
        if (transactions.Any(t => IsOffHours(t.CreatedAt)))
        {
            recommendations.Add("Enable transaction alerts for off-hours activity");
        }
        
        if (transactions.Any(t => t.Amount > 5000))
        {
            recommendations.Add("Enable two-factor authentication for high-value transactions");
        }
        
        return recommendations;
    }
}

public class FraudRiskAnalysis
{
    public string? TransactionId { get; set; }
    public string? PaymentId { get; set; }
    public double RiskScore { get; set; }
    public FraudRiskLevel RiskLevel { get; set; }
    public List<string> Factors { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}

public class SpendingAnalysis
{
    public string UserId { get; set; } = string.Empty;
    public DateRange Period { get; set; } = new();
    public decimal TotalSpent { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }
    public Dictionary<string, decimal> Categories { get; set; } = new();
    public List<SpendingTrend> Trends { get; set; } = new();
    public List<SpendingAnomaly> Anomalies { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}

public class PersonalizedInsights
{
    public string UserId { get; set; } = string.Empty;
    public List<string> BudgetRecommendations { get; set; } = new();
    public List<string> SavingsOpportunities { get; set; } = new();
    public List<string> SpendingAlerts { get; set; } = new();
    public List<string> SecurityRecommendations { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class FraudFeatures
{
    public decimal Amount { get; set; }
    public bool IsHighValue { get; set; }
    public bool IsUnusualAmount { get; set; }
    public bool IsOffHours { get; set; }
    public double VelocityScore { get; set; }
    public double LocationRisk { get; set; }
    public double DeviceRisk { get; set; }
    public double ProviderRisk { get; set; }
    public List<string> RiskFactors { get; set; } = new();
}

public class DateRange
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class SpendingTrend
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
}

public class SpendingAnomaly
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}

public class FraudFeedback
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string FeedbackType { get; set; } = string.Empty;
    public bool IsActualFraud { get; set; }
    public string Comments { get; set; } = string.Empty;
}

public enum FraudRiskLevel
{
    Minimal,
    Low,
    Medium,
    High,
    Critical
}

public static class FraudDetectionExtensions
{
    public static IServiceCollection AddFraudDetection(this IServiceCollection services)
    {
        services.AddScoped<IFraudDetectionService, FraudDetectionService>();
        return services;
    }
}
