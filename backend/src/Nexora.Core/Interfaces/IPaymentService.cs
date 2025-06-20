using Nexora.Core.DTOs;

namespace Nexora.Core.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentResult> RefundPaymentAsync(int paymentId, decimal amount, string reason);
        Task<PaymentResult> GetPaymentStatusAsync(string referenceId);
        Task<bool> ValidatePaymentAsync(PaymentRequest request);
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string ReferenceId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
