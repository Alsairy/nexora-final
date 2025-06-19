using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexora.Core.Configuration;
using Nexora.Core.Data;
using Nexora.Core.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Data.Migrations
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var environment = services.GetRequiredService<IWebHostEnvironment>();
            var seedingSettings = services.GetRequiredService<IOptions<SeedingSettings>>().Value;
            var logger = services.GetRequiredService<ILogger<NexoraDbContext>>();

            // Check if seeding is allowed in current environment
            if (!seedingSettings.EnabledEnvironments.Contains(environment.EnvironmentName))
            {
                logger.LogInformation("Seeding is not enabled for environment: {Environment}", environment.EnvironmentName);
                return;
            }

            try
            {
                var context = services.GetRequiredService<NexoraDbContext>();
                
                // Apply migrations
                await context.Database.MigrateAsync();
                
                // Seed data
                await SeedTenantsAsync(context, logger);
                await SeedUsersAsync(context, seedingSettings, logger);
                await SeedPaymentProvidersAsync(context, logger);
                await SeedSmsProvidersAsync(context, logger);
                
                logger.LogInformation("Seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        private static async Task SeedTenantsAsync(NexoraDbContext context, ILogger logger)
        {
            if (!await context.Tenants.AnyAsync())
            {
                logger.LogInformation("Seeding tenants");
                
                await context.Tenants.AddRangeAsync(
                    new Tenant
                    {
                        Name = "Default Tenant",
                        Subdomain = "default",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Tenant
                    {
                        Name = "Demo Tenant",
                        Subdomain = "demo",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                );
                
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedUsersAsync(NexoraDbContext context, SeedingSettings settings, ILogger logger)
        {
            if (!await context.Users.AnyAsync())
            {
                logger.LogInformation("Seeding users");
                
                var passwordHasher = new PasswordHasher();
                var hashedPassword = passwordHasher.HashPassword(settings.AdminUser.Password);
                
                var adminUser = new User
                {
                    Email = settings.AdminUser.Email,
                    FirstName = settings.AdminUser.FirstName,
                    LastName = settings.AdminUser.LastName,
                    PasswordHash = hashedPassword,
                    Role = "Admin",
                    IsActive = true,
                    TenantId = 1, // Default tenant
                    CreatedAt = DateTime.UtcNow
                };
                
                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedPaymentProvidersAsync(NexoraDbContext context, ILogger logger)
        {
            if (!await context.PaymentProviders.AnyAsync())
            {
                logger.LogInformation("Seeding payment providers");
                
                await context.PaymentProviders.AddRangeAsync(
                    new PaymentProvider
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Stripe",
                        DisplayName = "Stripe",
                        Description = "Global payment processing platform",
                        ApiEndpoint = "https://api.stripe.com/v1",
                        CountryCode = "US",
                        Currency = "USD",
                        IsActive = true,
                        SupportsCreditCards = true,
                        SupportsDebitCards = true,
                        SupportsBankTransfers = false,
                        SupportsDigitalWallets = true,
                        SupportsRecurringPayments = true,
                        TransactionFeePercentage = 2.9m,
                        FixedTransactionFee = 0.30m,
                        Priority = 1,
                        CreatedAt = DateTime.UtcNow
                    },
                    new PaymentProvider
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "PayPal",
                        DisplayName = "PayPal",
                        Description = "Digital payment platform",
                        ApiEndpoint = "https://api.paypal.com/v1",
                        CountryCode = "US",
                        Currency = "USD",
                        IsActive = true,
                        SupportsCreditCards = true,
                        SupportsDebitCards = true,
                        SupportsBankTransfers = true,
                        SupportsDigitalWallets = true,
                        SupportsRecurringPayments = true,
                        TransactionFeePercentage = 3.49m,
                        FixedTransactionFee = 0.49m,
                        Priority = 2,
                        CreatedAt = DateTime.UtcNow
                    },
                    new PaymentProvider
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Mada",
                        DisplayName = "Mada",
                        Description = "Saudi Arabia national payment scheme",
                        ApiEndpoint = "https://api.mada.com.sa/v1",
                        CountryCode = "SA",
                        Currency = "SAR",
                        IsActive = true,
                        SupportsCreditCards = false,
                        SupportsDebitCards = true,
                        SupportsBankTransfers = true,
                        SupportsDigitalWallets = false,
                        SupportsRecurringPayments = true,
                        TransactionFeePercentage = 1.75m,
                        FixedTransactionFee = 0.00m,
                        Priority = 1,
                        CreatedAt = DateTime.UtcNow
                    },
                    new PaymentProvider
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "HyperPay",
                        DisplayName = "HyperPay",
                        Description = "Middle East payment gateway",
                        ApiEndpoint = "https://oppwa.com/v1",
                        CountryCode = "SA",
                        Currency = "SAR",
                        IsActive = true,
                        SupportsCreditCards = true,
                        SupportsDebitCards = true,
                        SupportsBankTransfers = true,
                        SupportsDigitalWallets = true,
                        SupportsRecurringPayments = true,
                        TransactionFeePercentage = 2.75m,
                        FixedTransactionFee = 0.00m,
                        Priority = 2,
                        CreatedAt = DateTime.UtcNow
                    }
                );
                
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedSmsProvidersAsync(NexoraDbContext context, ILogger logger)
        {
            if (!await context.SmsProviders.AnyAsync())
            {
                logger.LogInformation("Seeding SMS providers");
                
                await context.SmsProviders.AddRangeAsync(
                    new SmsProvider
                    {
                        Id = 1,
                        Name = "Twilio",
                        Code = "TWILIO",
                        Description = "Global SMS provider with high delivery rates",
                        ApiEndpoint = "https://api.twilio.com/2010-04-01",
                        Country = "SA",
                        CostPerSms = 0.05m,
                        Currency = "SAR",
                        Priority = 1,
                        IsActive = true,
                        IsDefault = true,
                        SupportsDeliveryReceipts = true,
                        SupportsUnicode = true,
                        SupportsLongMessages = true,
                        MaxMessageLength = 1600,
                        MaxConcurrentMessages = 100,
                        RateLimitPerSecond = 10,
                        RateLimitPerMinute = 600,
                        RateLimitPerHour = 36000,
                        IsKsaCompliant = true,
                        SupportsCitcIntegration = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SmsProvider
                    {
                        Id = 2,
                        Name = "Unifonic",
                        Code = "UNIFONIC",
                        Description = "Regional SMS provider for MENA region",
                        ApiEndpoint = "https://api.unifonic.com/v1",
                        Country = "SA",
                        CostPerSms = 0.04m,
                        Currency = "SAR",
                        Priority = 2,
                        IsActive = true,
                        IsDefault = false,
                        SupportsDeliveryReceipts = true,
                        SupportsUnicode = true,
                        SupportsLongMessages = true,
                        MaxMessageLength = 1600,
                        MaxConcurrentMessages = 50,
                        RateLimitPerSecond = 5,
                        RateLimitPerMinute = 300,
                        RateLimitPerHour = 18000,
                        IsKsaCompliant = true,
                        SupportsCitcIntegration = true,
                        CreatedAt = DateTime.UtcNow
                    }
                );
                
                await context.SaveChangesAsync();
            }
        }
    }

    public class SeedingSettings
    {
        public string[] EnabledEnvironments { get; set; }
        public AdminUserSettings AdminUser { get; set; }
    }

    public class AdminUserSettings
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
    }
}

