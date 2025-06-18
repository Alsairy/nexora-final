using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using Nexora.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Services
{
    public class KeyVaultService : IKeyVaultService
    {
        private readonly KeyVaultSettings _settings;
        private readonly SecretClient _secretClient;
        private readonly ICryptoHelper _cryptoHelper;

        public KeyVaultService(
            IOptions<KeyVaultSettings> settings,
            ICryptoHelper cryptoHelper)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _cryptoHelper = cryptoHelper ?? throw new ArgumentNullException(nameof(cryptoHelper));

            // Create Key Vault client
            var credential = new DefaultAzureCredential();
            _secretClient = new SecretClient(new Uri(_settings.VaultUri), credential);
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            try
            {
                var response = await _secretClient.GetSecretAsync(secretName);
                return response.Value.Value;
            }
            catch (Exception ex)
            {
                throw new KeyVaultException($"Failed to retrieve secret '{secretName}': {ex.Message}", ex);
            }
        }

        public async Task<string> DecryptAsync(string encryptedValue)
        {
            // Try with current key first
            try
            {
                return await _cryptoHelper.DecryptAsync(encryptedValue, _settings.CurrentKeyId);
            }
            catch (Exception)
            {
                // If current key fails, try with previous keys
                if (_settings.PreviousKeyIds != null && _settings.PreviousKeyIds.Length > 0)
                {
                    foreach (var keyId in _settings.PreviousKeyIds)
                    {
                        try
                        {
                            return await _cryptoHelper.DecryptAsync(encryptedValue, keyId);
                        }
                        catch (Exception)
                        {
                            // Continue to next key
                            continue;
                        }
                    }
                }

                // If all keys fail, throw exception
                throw new KeyVaultException("Failed to decrypt value with any available keys");
            }
        }

        public async Task<string> EncryptAsync(string value)
        {
            try
            {
                return await _cryptoHelper.EncryptAsync(value, _settings.CurrentKeyId);
            }
            catch (Exception ex)
            {
                throw new KeyVaultException($"Failed to encrypt value: {ex.Message}", ex);
            }
        }

        public async Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames)
        {
            var result = new Dictionary<string, string>();

            foreach (var secretName in secretNames)
            {
                try
                {
                    var secret = await GetSecretAsync(secretName);
                    result.Add(secretName, secret);
                }
                catch (Exception ex)
                {
                    throw new KeyVaultException($"Failed to retrieve secret '{secretName}': {ex.Message}", ex);
                }
            }

            return result;
        }
    }

    public interface IKeyVaultService
    {
        Task<string> GetSecretAsync(string secretName);
        Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames);
        Task<string> DecryptAsync(string encryptedValue);
        Task<string> EncryptAsync(string value);
    }

    public class KeyVaultException : Exception
    {
        public KeyVaultException(string message) : base(message)
        {
        }

        public KeyVaultException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

