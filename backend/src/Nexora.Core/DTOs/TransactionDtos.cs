using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.DTOs
{
    public class CreateTransactionDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(3)]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency must be a valid 3-letter ISO code")]
        public string Currency { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }
    }

    public class UpdateTransactionStatusDto
    {
        [Required]
        [MaxLength(50)]
        public string Status { get; set; }
    }

    public class TransactionDto
    {
        public int Id { get; set; }
        public string ReferenceId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TransactionSummaryDto
    {
        public int TotalTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; }
        public Dictionary<string, int> StatusBreakdown { get; set; }
        public Dictionary<string, int> TypeBreakdown { get; set; }
    }
}
