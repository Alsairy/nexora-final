using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexora.Core.Configuration;

public static class SecretsConfiguration
{
    public static IServiceCollection AddSecretsManagement(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            ValidateEnvironmentVariables(configuration);
        }
        
        services.Configure<DatabaseSecrets>(options =>
        {
            options.ConnectionString = GetRequiredSecret(configuration, "ConnectionStrings:DefaultConnection");
            options.RedisConnectionString = GetRequiredSecret(configuration, "ConnectionStrings:Redis");
        });
        
        services.Configure<JwtSecrets>(options =>
        {
            options.SecretKey = GetRequiredSecret(configuration, "Jwt:SecretKey");
            options.Issuer = GetRequiredSecret(configuration, "Jwt:Issuer");
            options.Audience = GetRequiredSecret(configuration, "Jwt:Audience");
        });
        
        services.Configure<EncryptionSecrets>(options =>
        {
            options.FieldKey = GetRequiredSecret(configuration, "Encryption:FieldKey");
            options.FieldIV = GetRequiredSecret(configuration, "Encryption:FieldIV");
        });
        
        services.Configure<ExternalServiceSecrets>(options =>
        {
            options.OpenAIApiKey = GetOptionalSecret(configuration, "OpenAI:ApiKey");
            options.StripeSecretKey = GetOptionalSecret(configuration, "Stripe:SecretKey");
            options.TwilioAuthToken = GetOptionalSecret(configuration, "Twilio:AuthToken");
        });
        
        return services;
    }
    
    private static string GetRequiredSecret(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Required configuration '{key}' is missing. Please set the environment variable or add it to your configuration.");
        }
        
        if (IsPlaceholderValue(value))
        {
            throw new InvalidOperationException($"Configuration '{key}' contains a placeholder value. Please set a real value.");
        }
        
        return value;
    }
    
    private static string? GetOptionalSecret(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        return IsPlaceholderValue(value) ? null : value;
    }
    
    private static bool IsPlaceholderValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
            
        var placeholders = new[]
        {
            "your-secret-key",
            "your-api-key",
            "your-connection-string",
            "changeme",
            "postgres123",
            "redis123",
            "your-256-bit-secret-key",
            "your-openai-api-key",
            "your-stripe-secret-key",
            "your-twilio-auth-token"
        };
        
        return placeholders.Any(placeholder => 
            value.Contains(placeholder, StringComparison.OrdinalIgnoreCase));
    }
    
    private static void ValidateEnvironmentVariables(IConfiguration configuration)
    {
        var requiredSecrets = new[]
        {
            "ConnectionStrings:DefaultConnection",
            "ConnectionStrings:Redis",
            "Jwt:SecretKey",
            "Jwt:Issuer",
            "Jwt:Audience",
            "Encryption:FieldKey",
            "Encryption:FieldIV"
        };
        
        var missingSecrets = new List<string>();
        var placeholderSecrets = new List<string>();
        
        foreach (var secret in requiredSecrets)
        {
            var value = configuration[secret];
            if (string.IsNullOrEmpty(value))
            {
                missingSecrets.Add(secret);
            }
            else if (IsPlaceholderValue(value))
            {
                placeholderSecrets.Add(secret);
            }
        }
        
        if (missingSecrets.Any() || placeholderSecrets.Any())
        {
            var logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger(typeof(SecretsConfiguration));
            
            if (missingSecrets.Any())
            {
                logger.LogWarning("Missing required secrets: {Secrets}", string.Join(", ", missingSecrets));
            }
            
            if (placeholderSecrets.Any())
            {
                logger.LogWarning("Placeholder values detected for: {Secrets}", string.Join(", ", placeholderSecrets));
            }
            
            logger.LogInformation("Please check your .env file or environment variables");
        }
    }
}

public class DatabaseSecrets
{
    public string ConnectionString { get; set; } = string.Empty;
    public string RedisConnectionString { get; set; } = string.Empty;
}

public class JwtSecrets
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}

public class EncryptionSecrets
{
    public string FieldKey { get; set; } = string.Empty;
    public string FieldIV { get; set; } = string.Empty;
}

public class ExternalServiceSecrets
{
    public string? OpenAIApiKey { get; set; }
    public string? StripeSecretKey { get; set; }
    public string? TwilioAuthToken { get; set; }
}
