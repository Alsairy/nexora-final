using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;

namespace Nexora.Core.Security;

public interface IPasswordHashingService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
    bool NeedsRehash(string hashedPassword);
}

public class Argon2PasswordHashingService : IPasswordHashingService
{
    private readonly int _memoryCost;
    private readonly int _timeCost;
    private readonly int _parallelism;
    private readonly int _hashLength;
    private readonly int _saltLength;

    public Argon2PasswordHashingService(IConfiguration configuration)
    {
        var section = configuration.GetSection("PasswordHashing:Argon2");
        _memoryCost = section.GetValue("MemoryCost", 65536); // 64 MB
        _timeCost = section.GetValue("TimeCost", 3);
        _parallelism = section.GetValue("Parallelism", 1);
        _hashLength = section.GetValue("HashLength", 32);
        _saltLength = section.GetValue("SaltLength", 16);
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        var salt = GenerateSalt();
        var hash = HashPasswordWithSalt(password, salt);
        
        return $"$argon2id$v=19$m={_memoryCost},t={_timeCost},p={_parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            return false;

        try
        {
            var parts = hashedPassword.Split('$');
            if (parts.Length != 6 || parts[1] != "argon2id")
                return false;

            var parameters = ParseParameters(parts[4]);
            var salt = Convert.FromBase64String(parts[5]);
            var expectedHash = Convert.FromBase64String(parts[6]);

            var actualHash = HashPasswordWithSalt(password, salt, parameters.memoryCost, parameters.timeCost, parameters.parallelism);
            
            return SlowEquals(expectedHash, actualHash);
        }
        catch
        {
            return false;
        }
    }

    public bool NeedsRehash(string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword))
            return true;

        try
        {
            var parts = hashedPassword.Split('$');
            if (parts.Length != 6 || parts[1] != "argon2id")
                return true;

            var parameters = ParseParameters(parts[4]);
            
            return parameters.memoryCost < _memoryCost ||
                   parameters.timeCost < _timeCost ||
                   parameters.parallelism < _parallelism;
        }
        catch
        {
            return true;
        }
    }

    private byte[] GenerateSalt()
    {
        var salt = new byte[_saltLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    private byte[] HashPasswordWithSalt(string password, byte[] salt, int? memoryCost = null, int? timeCost = null, int? parallelism = null)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        
        using var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, salt, timeCost ?? _timeCost, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(_hashLength);
    }

    private (int memoryCost, int timeCost, int parallelism) ParseParameters(string paramString)
    {
        var parts = paramString.Split(',');
        var memoryCost = int.Parse(parts[0].Split('=')[1]);
        var timeCost = int.Parse(parts[1].Split('=')[1]);
        var parallelism = int.Parse(parts[2].Split('=')[1]);
        
        return (memoryCost, timeCost, parallelism);
    }

    private static bool SlowEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        var diff = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }

        return diff == 0;
    }
}

public static class PasswordSecurityExtensions
{
    public static IServiceCollection AddPasswordSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHashingService, Argon2PasswordHashingService>();
        return services;
    }
}
