using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.DTOs
{
    public class CreatePaymentDto
    {
        [Required]
        public int TransactionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(3)]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency must be a valid 3-letter ISO code")]
        public string Currency { get; set; }

        [MaxLength(100)]
        public string ExternalReference { get; set; }

        [Required]
        [MaxLength(100)]
        public string IdempotencyKey { get; set; }
    }

    public class PaymentDto
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string ExternalReference { get; set; }
        public string IdempotencyKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class RefundPaymentDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }
    }

    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string ReferenceId { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public Dictionary<string, string> AdditionalData { get; set; }
    }
}
