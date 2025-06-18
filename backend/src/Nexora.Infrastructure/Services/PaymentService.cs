using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexora.Core.Data;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly NexoraDbContext _dbContext;
        private readonly ILogger<PaymentService> _logger;
        private readonly ICacheService _cacheService;

        public PaymentService(
            NexoraDbContext dbContext,
            ILogger<PaymentService> logger,
            ICacheService cacheService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _cacheService = cacheService;
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
                CreatedAt = DateTime.UtcNow
            };

            try
            {
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

    public interface IPaymentService
    {
        Task<Payment> ProcessPaymentAsync(PaymentRequest request);
        Task<Payment> RefundPaymentAsync(RefundRequest request);
    }
}

