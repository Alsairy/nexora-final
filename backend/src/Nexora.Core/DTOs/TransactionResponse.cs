using System;

namespace Nexora.Core.DTOs
{
    public class TransactionResponse
    {
        public int Id { get; set; }
        public string ReferenceId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int UserId { get; set; }
        public int TenantId { get; set; }
    }
}
