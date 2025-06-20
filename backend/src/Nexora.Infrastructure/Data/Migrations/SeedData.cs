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
using BCrypt.Net;

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
            if (seedingSettings.EnabledEnvironments == null || !seedingSettings.EnabledEnvironments.Contains(environment.EnvironmentName))
            {
                logger.LogInformation("Seeding is not enabled for environment: {Environment}. Forcing seeding for Production.", environment.EnvironmentName);
                if (environment.EnvironmentName != "Production")
                {
                    return;
                }
            }

            try
            {
                var context = services.GetRequiredService<NexoraDbContext>();
                
                logger.LogInformation("Ensuring database is created...");
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Database creation completed");
                
                // Seed data
                await SeedTenantsAsync(context, logger);
                await SeedUsersAsync(context, seedingSettings, logger);
                
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
            try
            {
                // Check if any tenants exist
                var existingTenants = await context.Tenants.CountAsync();
                if (existingTenants == 0)
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
                    logger.LogInformation("Seeded {TenantCount} tenants successfully", 2);
                }
                else
                {
                    logger.LogInformation("Tenants already exist, skipping seeding");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding tenants");
                throw;
            }
        }

        private static async Task SeedUsersAsync(NexoraDbContext context, SeedingSettings settings, ILogger logger)
        {
            try
            {
                // Check if any users exist
                var existingUsers = await context.Users.CountAsync();
                if (existingUsers == 0)
                {
                    logger.LogInformation("Seeding users");
                    
                    // Check if settings are null
                    if (settings?.AdminUser == null)
                    {
                        logger.LogWarning("AdminUser settings are null, using default values");
                        settings = new SeedingSettings
                        {
                            AdminUser = new AdminUserSettings
                            {
                                Email = "admin@nexora.com",
                                FirstName = "Admin",
                                LastName = "User",
                                Password = "Admin123!"
                            }
                        };
                    }
                    
                    var adminHashedPassword = BCrypt.Net.BCrypt.HashPassword(settings.AdminUser.Password);
                    var adminUser = new User
                    {
                        Email = settings.AdminUser.Email,
                        FirstName = settings.AdminUser.FirstName,
                        LastName = settings.AdminUser.LastName,
                        PasswordHash = adminHashedPassword,
                        Role = "Admin",
                        IsActive = true,
                        TenantId = 1, // Default tenant
                        CreatedAt = DateTime.UtcNow,
                        PhoneNumber = null // Explicitly set to null to avoid schema issues
                    };
                    
                    var demoHashedPassword = BCrypt.Net.BCrypt.HashPassword("demo123");
                    var demoUser = new User
                    {
                        Email = "demo@nexora.com",
                        FirstName = "Demo",
                        LastName = "User",
                        PasswordHash = demoHashedPassword,
                        Role = "User",
                        IsActive = true,
                        TenantId = 1, // Default tenant
                        CreatedAt = DateTime.UtcNow,
                        PhoneNumber = null // Explicitly set to null to avoid schema issues
                    };
                    
                    var testHashedPassword = BCrypt.Net.BCrypt.HashPassword("TestPassword123!");
                    var testUser = new User
                    {
                        Email = "test@nexora.com",
                        FirstName = "Test",
                        LastName = "User",
                        PasswordHash = testHashedPassword,
                        Role = "User",
                        IsActive = true,
                        TenantId = 1, // Default tenant
                        CreatedAt = DateTime.UtcNow,
                        PhoneNumber = null // Explicitly set to null to avoid schema issues
                    };
                    
                    await context.Users.AddRangeAsync(adminUser, demoUser, testUser);
                    await context.SaveChangesAsync();
                    
                    logger.LogInformation("Seeded {UserCount} users successfully", 3);
                }
                else
                {
                    logger.LogInformation("Users already exist, skipping seeding");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding users");
                throw;
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

