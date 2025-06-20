using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexora.Core.Data;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Core.DTOs;
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

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            // Validate request
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Validate basic payment request properties
            if (request.Amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero", nameof(request.Amount));
            }

            // Check for existing payment with the same external reference
            var existingPayment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.ExternalReference == request.ExternalReference && !string.IsNullOrEmpty(request.ExternalReference));

            if (existingPayment != null)
            {
                _logger.LogInformation("Payment with external reference {ExternalReference} already exists", request.ExternalReference);
                return new PaymentResult
                {
                    Success = true,
                    ReferenceId = existingPayment.ExternalReference ?? existingPayment.Id.ToString(),
                    Status = existingPayment.Status,
                    Amount = existingPayment.Amount,
                    Currency = existingPayment.Currency
                };
            }

            // Create payment - using a default transaction ID since PaymentRequest doesn't have TransactionId
            var payment = new Payment
            {
                TransactionId = 1, // Default transaction ID - this should be passed differently in a real implementation
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = "Pending",
                IdempotencyKey = Guid.NewGuid().ToString(), // Generate idempotency key since PaymentRequest doesn't have it
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                // Process payment with external provider
                var result = await ProcessWithExternalProviderAsync(payment);
                
                // Update payment status
                payment.Status = result.Success ? "Completed" : "Failed";
                payment.ExternalReference = result.ReferenceId;
                
                // Update payment status based on result
                payment.Status = result.Success ? "Completed" : "Failed";
                payment.ExternalReference = result.ReferenceId;
                
                // Save payment
                await _dbContext.Payments.AddAsync(payment);
                await _dbContext.SaveChangesAsync();
                
                return new PaymentResult
                {
                    Success = true,
                    ReferenceId = payment.ExternalReference ?? payment.Id.ToString(),
                    Status = payment.Status,
                    Amount = payment.Amount,
                    Currency = payment.Currency
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for amount {Amount}", request.Amount);
                
                // Save failed payment
                payment.Status = "Failed";
                await _dbContext.Payments.AddAsync(payment);
                await _dbContext.SaveChangesAsync();
                
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<PaymentResult> RefundPaymentAsync(int paymentId, decimal amount, string reason)
        {
            // Get original payment
            var originalPayment = await _dbContext.Payments.FindAsync(paymentId);
            if (originalPayment == null)
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Payment with ID {paymentId} not found"
                };
            }

            if (originalPayment.Status != "Completed")
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Cannot refund payment with status {originalPayment.Status}"
                };
            }

            // Get transaction
            var transaction = await _dbContext.Transactions.FindAsync(originalPayment.TransactionId);
            if (transaction == null)
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Transaction not found for payment {paymentId}"
                };
            }

            // Create refund payment
            var refund = new Payment
            {
                TransactionId = originalPayment.TransactionId,
                PaymentMethod = originalPayment.PaymentMethod,
                Amount = amount,
                Currency = originalPayment.Currency,
                Status = "Pending",
                IdempotencyKey = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                OriginalPaymentId = originalPayment.Id
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
                
                return new PaymentResult
                {
                    Success = true,
                    ReferenceId = refund.ExternalReference ?? refund.Id.ToString(),
                    Status = refund.Status,
                    Amount = refund.Amount,
                    Currency = refund.Currency
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
                
                // Save failed refund
                refund.Status = "Failed";
                await _dbContext.Payments.AddAsync(refund);
                await _dbContext.SaveChangesAsync();
                
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
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

        public async Task<PaymentResult> GetPaymentStatusAsync(string referenceId)
        {
            if (string.IsNullOrEmpty(referenceId))
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Reference ID is required"
                };
            }

            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.ExternalReference == referenceId || p.Id.ToString() == referenceId);

            if (payment == null)
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Payment with reference ID {referenceId} not found"
                };
            }

            return new PaymentResult
            {
                Success = true,
                ReferenceId = payment.ExternalReference ?? payment.Id.ToString(),
                Status = payment.Status,
                Amount = payment.Amount,
                Currency = payment.Currency
            };
        }

        public async Task<bool> ValidatePaymentAsync(PaymentRequest request)
        {
            if (request == null)
                return false;

            if (request.Amount <= 0)
                return false;

            if (string.IsNullOrEmpty(request.Currency))
                return false;

            if (string.IsNullOrEmpty(request.PaymentMethod))
                return false;

            // Check for duplicate external reference if provided
            if (!string.IsNullOrEmpty(request.ExternalReference))
            {
                var existingPayment = await _dbContext.Payments
                    .AnyAsync(p => p.ExternalReference == request.ExternalReference);
                
                if (existingPayment)
                    return false;
            }

            return true;
        }

        public async Task<PaymentResult> GetPaymentAsync(int paymentId)
        {
            var payment = await _dbContext.Payments.FindAsync(paymentId);
            
            if (payment == null)
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Payment with ID {paymentId} not found"
                };
            }

            return new PaymentResult
            {
                Success = true,
                ReferenceId = payment.ExternalReference ?? payment.Id.ToString(),
                Status = payment.Status,
                Amount = payment.Amount,
                Currency = payment.Currency
            };
        }

        public async Task<List<PaymentResult>> GetUserPaymentsAsync(int userId)
        {
            var payments = await _dbContext.Payments
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return payments.Select(p => new PaymentResult
            {
                Success = true,
                ReferenceId = p.ExternalReference ?? p.Id.ToString(),
                Status = p.Status,
                Amount = p.Amount,
                Currency = p.Currency
            }).ToList();
        }
    }








}

