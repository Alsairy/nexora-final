using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Options;
using Nexora.Core.Configuration;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Services
{
    public class CryptoHelper : ICryptoHelper
    {
        private readonly KeyVaultSettings _settings;
        private readonly CryptographyClientCache _clientCache;

        public CryptoHelper(IOptions<KeyVaultSettings> settings)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _clientCache = new CryptographyClientCache();
        }

        public async Task<string> EncryptAsync(string plainText, string keyId)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            var client = await GetCryptographyClientAsync(keyId);
            
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptResult = await client.EncryptAsync(EncryptionAlgorithm.RsaOaep, plainBytes);
            
            return Convert.ToBase64String(encryptResult.Ciphertext);
        }

        public async Task<string> DecryptAsync(string encryptedText, string keyId)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            var client = await GetCryptographyClientAsync(keyId);
            
            var cipherBytes = Convert.FromBase64String(encryptedText);
            var decryptResult = await client.DecryptAsync(EncryptionAlgorithm.RsaOaep, cipherBytes);
            
            return Encoding.UTF8.GetString(decryptResult.Plaintext);
        }

        private async Task<CryptographyClient> GetCryptographyClientAsync(string keyId)
        {
            // Check if client exists in cache
            if (_clientCache.TryGetClient(keyId, out var cachedClient))
            {
                return cachedClient;
            }

            // Create new client
            var credential = new DefaultAzureCredential();
            var keyClient = new KeyClient(new Uri(_settings.VaultUri), credential);
            var key = await keyClient.GetKeyAsync(keyId);
            var client = new CryptographyClient(key.Value.Id, credential);
            
            // Add to cache
            _clientCache.AddClient(keyId, client);
            
            return client;
        }

        // Simple cache for CryptographyClient instances
        private class CryptographyClientCache
        {
            private readonly Dictionary<string, CryptographyClient> _clients = new Dictionary<string, CryptographyClient>();

            public bool TryGetClient(string keyId, out CryptographyClient client)
            {
                return _clients.TryGetValue(keyId, out client);
            }

            public void AddClient(string keyId, CryptographyClient client)
            {
                _clients[keyId] = client;
            }
        }
    }

    public interface ICryptoHelper
    {
        Task<string> EncryptAsync(string plainText, string keyId);
        Task<string> DecryptAsync(string encryptedText, string keyId);
    }
}

