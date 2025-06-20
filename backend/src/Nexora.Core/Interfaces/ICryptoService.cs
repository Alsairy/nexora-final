using System.Threading.Tasks;

namespace Nexora.Core.Interfaces;

public interface ICryptoService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string cipherText);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    Task<string> GenerateSecureTokenAsync();
    Task<bool> ValidateTokenAsync(string token);
}
