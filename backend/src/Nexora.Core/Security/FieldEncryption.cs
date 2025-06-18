using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Core.Security;

public interface IFieldEncryptionService
{
    string EncryptField(string plainText);
    string DecryptField(string encryptedText);
    T EncryptSensitiveFields<T>(T entity) where T : class;
    T DecryptSensitiveFields<T>(T entity) where T : class;
}

public class FieldEncryptionService : IFieldEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public FieldEncryptionService(IConfiguration configuration)
    {
        var encryptionKey = configuration["Encryption:FieldKey"] ?? throw new InvalidOperationException("Field encryption key not configured");
        var encryptionIv = configuration["Encryption:FieldIV"] ?? throw new InvalidOperationException("Field encryption IV not configured");
        
        _key = Convert.FromBase64String(encryptionKey);
        _iv = Convert.FromBase64String(encryptionIv);
    }

    public string EncryptField(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using var swEncrypt = new StreamWriter(csEncrypt);
        
        swEncrypt.Write(plainText);
        swEncrypt.Close();
        
        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string DecryptField(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedText));
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch
        {
            return encryptedText;
        }
    }

    public T EncryptSensitiveFields<T>(T entity) where T : class
    {
        if (entity == null) return entity;

        var properties = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(EncryptedFieldAttribute), false).Any())
            .Where(p => p.PropertyType == typeof(string) && p.CanWrite);

        foreach (var property in properties)
        {
            var value = property.GetValue(entity) as string;
            if (!string.IsNullOrEmpty(value))
            {
                property.SetValue(entity, EncryptField(value));
            }
        }

        return entity;
    }

    public T DecryptSensitiveFields<T>(T entity) where T : class
    {
        if (entity == null) return entity;

        var properties = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(EncryptedFieldAttribute), false).Any())
            .Where(p => p.PropertyType == typeof(string) && p.CanWrite);

        foreach (var property in properties)
        {
            var value = property.GetValue(entity) as string;
            if (!string.IsNullOrEmpty(value))
            {
                property.SetValue(entity, DecryptField(value));
            }
        }

        return entity;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class EncryptedFieldAttribute : Attribute
{
    public string Purpose { get; set; } = "PII";
    public bool Required { get; set; } = false;
}

public static class EncryptionExtensions
{
    public static IServiceCollection AddFieldEncryption(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IFieldEncryptionService, FieldEncryptionService>();
        return services;
    }
}
