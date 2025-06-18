using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nexora.Core.AI;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Core.Security;
using Nexora.Infrastructure.Services;
using System.Linq.Expressions;
using Xunit;

namespace Nexora.Tests.Unit;

public class PaymentServiceTests
{
    private readonly Mock<IRepository<Payment>> _paymentRepositoryMock;
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IFraudDetectionService> _fraudDetectionMock;
    private readonly Mock<IComprehensiveAuditService> _auditServiceMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _paymentRepositoryMock = new Mock<IRepository<Payment>>();
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _fraudDetectionMock = new Mock<IFraudDetectionService>();
        _auditServiceMock = new Mock<IComprehensiveAuditService>();
        _loggerMock = new Mock<ILogger<PaymentService>>();
        _tenantServiceMock = new Mock<ITenantService>();

        _tenantServiceMock.Setup(x => x.GetCurrentTenantId()).Returns("tenant-1");

        _paymentService = new PaymentService(
            _paymentRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _fraudDetectionMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object,
            _tenantServiceMock.Object
        );
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidPayment_ShouldSucceed()
    {
        var payment = new Payment
        {
            Id = "payment-1",
            UserId = "user-1",
            Amount = 100.00m,
            Currency = "USD",
            Provider = "stripe",
            Status = PaymentStatus.Pending,
            TenantId = "tenant-1"
        };

        var fraudAnalysis = new FraudRiskAnalysis
        {
            PaymentId = payment.Id,
            RiskLevel = FraudRiskLevel.Low,
            RiskScore = 0.1,
            Factors = new List<string>(),
            Recommendations = new List<string> { "Standard processing" }
        };

        _fraudDetectionMock
            .Setup(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()))
            .ReturnsAsync(fraudAnalysis);

        _paymentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Payment>()))
            .Returns(Task.CompletedTask);

        _paymentRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _paymentService.ProcessPaymentAsync(payment);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Completed, result.Payment.Status);
        Assert.NotNull(result.Payment.ProcessedAt);

        _fraudDetectionMock.Verify(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()), Times.Once);
        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Once);
        _paymentRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        _auditServiceMock.Verify(x => x.LogUserActionAsync(
            "PaymentProcessed", 
            It.IsAny<object>(), 
            payment.UserId, 
            null), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_HighRiskFraud_ShouldBlock()
    {
        var payment = new Payment
        {
            Id = "payment-2",
            UserId = "user-2",
            Amount = 10000.00m,
            Currency = "USD",
            Provider = "stripe",
            Status = PaymentStatus.Pending,
            TenantId = "tenant-1"
        };

        var fraudAnalysis = new FraudRiskAnalysis
        {
            PaymentId = payment.Id,
            RiskLevel = FraudRiskLevel.Critical,
            RiskScore = 0.9,
            Factors = new List<string> { "High value transaction", "Suspicious location" },
            Recommendations = new List<string> { "Block transaction immediately", "Require manual review" }
        };

        _fraudDetectionMock
            .Setup(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()))
            .ReturnsAsync(fraudAnalysis);

        var result = await _paymentService.ProcessPaymentAsync(payment);

        Assert.False(result.IsSuccess);
        Assert.Equal(PaymentStatus.Blocked, result.Payment.Status);
        Assert.Contains("fraud", result.ErrorMessage.ToLower());

        _fraudDetectionMock.Verify(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()), Times.Once);
        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Never);
        _auditServiceMock.Verify(x => x.LogSecurityEventAsync(
            SecurityEventType.SuspiciousActivity,
            It.IsAny<string>(),
            It.IsAny<object>(),
            payment.UserId), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_DuplicateIdempotencyKey_ShouldReturnExisting()
    {
        var idempotencyKey = "idem-key-123";
        var existingPayment = new Payment
        {
            Id = "payment-existing",
            UserId = "user-1",
            Amount = 100.00m,
            Currency = "USD",
            Provider = "stripe",
            Status = PaymentStatus.Completed,
            IdempotencyKey = idempotencyKey,
            TenantId = "tenant-1"
        };

        var newPayment = new Payment
        {
            Id = "payment-new",
            UserId = "user-1",
            Amount = 100.00m,
            Currency = "USD",
            Provider = "stripe",
            Status = PaymentStatus.Pending,
            IdempotencyKey = idempotencyKey,
            TenantId = "tenant-1"
        };

        _paymentRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
            .ReturnsAsync(new List<Payment> { existingPayment });

        var result = await _paymentService.ProcessPaymentAsync(newPayment);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingPayment.Id, result.Payment.Id);
        Assert.Equal(PaymentStatus.Completed, result.Payment.Status);

        _fraudDetectionMock.Verify(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()), Times.Never);
        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidAmount_ShouldFail()
    {
        var payment = new Payment
        {
            Id = "payment-invalid",
            UserId = "user-1",
            Amount = -50.00m, // Invalid negative amount
            Currency = "USD",
            Provider = "stripe",
            Status = PaymentStatus.Pending,
            TenantId = "tenant-1"
        };

        var result = await _paymentService.ProcessPaymentAsync(payment);

        Assert.False(result.IsSuccess);
        Assert.Contains("amount", result.ErrorMessage.ToLower());

        _fraudDetectionMock.Verify(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()), Times.Never);
        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_UnsupportedCurrency_ShouldFail()
    {
        var payment = new Payment
        {
            Id = "payment-currency",
            UserId = "user-1",
            Amount = 100.00m,
            Currency = "XYZ", // Unsupported currency
            Provider = "stripe",
            Status = PaymentStatus.Pending,
            TenantId = "tenant-1"
        };

        var result = await _paymentService.ProcessPaymentAsync(payment);

        Assert.False(result.IsSuccess);
        Assert.Contains("currency", result.ErrorMessage.ToLower());

        _fraudDetectionMock.Verify(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()), Times.Never);
        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_MediumRiskFraud_ShouldRequireAdditionalAuth()
    {
        var payment = new Payment
        {
            Id = "payment-medium-risk",
            UserId = "user-3",
            Amount = 1000.00m,
            Currency = "USD",
            Provider = "stripe",
            Status = PaymentStatus.Pending,
            TenantId = "tenant-1"
        };

        var fraudAnalysis = new FraudRiskAnalysis
        {
            PaymentId = payment.Id,
            RiskLevel = FraudRiskLevel.Medium,
            RiskScore = 0.5,
            Factors = new List<string> { "Unusual amount for user" },
            Recommendations = new List<string> { "Require additional authentication" }
        };

        _fraudDetectionMock
            .Setup(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()))
            .ReturnsAsync(fraudAnalysis);

        var result = await _paymentService.ProcessPaymentAsync(payment);

        Assert.False(result.IsSuccess);
        Assert.Equal(PaymentStatus.PendingAuth, result.Payment.Status);
        Assert.Contains("additional authentication", result.ErrorMessage.ToLower());

        _fraudDetectionMock.Verify(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()), Times.Once);
        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Once);
    }

    [Fact]
    public async Task GetPaymentAsync_ExistingPayment_ShouldReturnPayment()
    {
        var paymentId = "payment-123";
        var payment = new Payment
        {
            Id = paymentId,
            UserId = "user-1",
            Amount = 100.00m,
            Currency = "USD",
            Provider = "stripe",
            Status = PaymentStatus.Completed,
            TenantId = "tenant-1"
        };

        _paymentRepositoryMock
            .Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        var result = await _paymentService.GetPaymentAsync(paymentId);

        Assert.NotNull(result);
        Assert.Equal(paymentId, result.Id);
        Assert.Equal(PaymentStatus.Completed, result.Status);

        _paymentRepositoryMock.Verify(x => x.GetByIdAsync(paymentId), Times.Once);
    }

    [Fact]
    public async Task GetPaymentAsync_NonExistentPayment_ShouldReturnNull()
    {
        var paymentId = "non-existent";

        _paymentRepositoryMock
            .Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync((Payment?)null);

        var result = await _paymentService.GetPaymentAsync(paymentId);

        Assert.Null(result);

        _paymentRepositoryMock.Verify(x => x.GetByIdAsync(paymentId), Times.Once);
    }

    [Fact]
    public async Task GetUserPaymentsAsync_ValidUser_ShouldReturnPayments()
    {
        var userId = "user-1";
        var payments = new List<Payment>
        {
            new Payment { Id = "payment-1", UserId = userId, Amount = 100.00m, Status = PaymentStatus.Completed, TenantId = "tenant-1" },
            new Payment { Id = "payment-2", UserId = userId, Amount = 200.00m, Status = PaymentStatus.Completed, TenantId = "tenant-1" }
        };

        _paymentRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
            .ReturnsAsync(payments);

        var result = await _paymentService.GetUserPaymentsAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(userId, p.UserId));

        _paymentRepositoryMock.Verify(x => x.GetAsync(It.IsAny<Expression<Func<Payment, bool>>>()), Times.Once);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(999.99)]
    [InlineData(10000.00)]
    public async Task ProcessPaymentAsync_ValidAmounts_ShouldSucceed(decimal amount)
    {
        var payment = new Payment
        {
            Id = $"payment-{amount}",
            UserId = "user-1",
            Amount = amount,
            Currency = "USD",
            Provider = "stripe",
            Status = PaymentStatus.Pending,
            TenantId = "tenant-1"
        };

        var fraudAnalysis = new FraudRiskAnalysis
        {
            PaymentId = payment.Id,
            RiskLevel = FraudRiskLevel.Low,
            RiskScore = 0.1,
            Factors = new List<string>(),
            Recommendations = new List<string> { "Standard processing" }
        };

        _fraudDetectionMock
            .Setup(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()))
            .ReturnsAsync(fraudAnalysis);

        _paymentRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _paymentService.ProcessPaymentAsync(payment);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Completed, result.Payment.Status);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("CAD")]
    public async Task ProcessPaymentAsync_SupportedCurrencies_ShouldSucceed(string currency)
    {
        var payment = new Payment
        {
            Id = $"payment-{currency}",
            UserId = "user-1",
            Amount = 100.00m,
            Currency = currency,
            Provider = "stripe",
            Status = PaymentStatus.Pending,
            TenantId = "tenant-1"
        };

        var fraudAnalysis = new FraudRiskAnalysis
        {
            PaymentId = payment.Id,
            RiskLevel = FraudRiskLevel.Low,
            RiskScore = 0.1,
            Factors = new List<string>(),
            Recommendations = new List<string> { "Standard processing" }
        };

        _fraudDetectionMock
            .Setup(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()))
            .ReturnsAsync(fraudAnalysis);

        _paymentRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _paymentService.ProcessPaymentAsync(payment);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Completed, result.Payment.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_DatabaseError_ShouldHandleGracefully()
    {
        var payment = new Payment
        {
            Id = "payment-db-error",
            UserId = "user-1",
            Amount = 100.00m,
            Currency = "USD",
            Provider = "stripe",
            Status = PaymentStatus.Pending,
            TenantId = "tenant-1"
        };

        var fraudAnalysis = new FraudRiskAnalysis
        {
            PaymentId = payment.Id,
            RiskLevel = FraudRiskLevel.Low,
            RiskScore = 0.1,
            Factors = new List<string>(),
            Recommendations = new List<string> { "Standard processing" }
        };

        _fraudDetectionMock
            .Setup(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()))
            .ReturnsAsync(fraudAnalysis);

        _paymentRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var result = await _paymentService.ProcessPaymentAsync(payment);

        Assert.False(result.IsSuccess);
        Assert.Contains("database", result.ErrorMessage.ToLower());

        _fraudDetectionMock.Verify(x => x.AnalyzePaymentAsync(It.IsAny<Payment>()), Times.Once);
        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Once);
    }
}

public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public Payment Payment { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Blocked,
    PendingAuth,
    Cancelled
}
