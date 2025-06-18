namespace Nexora.Core.Configuration
{
    public class TenancySettings
    {
        public string Mode { get; set; } // Subdomain or Header
        public string HeaderName { get; set; } // X-Tenant-ID
        public int DefaultTenantId { get; set; } // Default tenant ID
    }
}

