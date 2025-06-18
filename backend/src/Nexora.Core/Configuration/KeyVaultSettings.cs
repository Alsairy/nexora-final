namespace Nexora.Core.Configuration
{
    public class KeyVaultSettings
    {
        public string VaultUri { get; set; }
        public string CurrentKeyId { get; set; }
        public string[] PreviousKeyIds { get; set; } // Added support for previous key IDs
    }
}

