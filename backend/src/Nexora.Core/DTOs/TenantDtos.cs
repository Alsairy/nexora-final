using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.DTOs
{
    public class CreateTenantDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Subdomain can only contain lowercase letters, numbers, and hyphens")]
        public string Subdomain { get; set; }
    }

    public class UpdateTenantDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public bool IsActive { get; set; }
    }

    public class TenantDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subdomain { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int UserCount { get; set; }
        public int TransactionCount { get; set; }
    }

    public class TenantStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalTransactionAmount { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalPaymentAmount { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
