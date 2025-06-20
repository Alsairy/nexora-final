using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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
            if (seedingSettings.EnabledEnvironments == null || !seedingSettings.EnabledEnvironments.Contains(environment.EnvironmentName))
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
                
                var passwordHasher = new PasswordHasher<User>();
                
                var adminHashedPassword = passwordHasher.HashPassword(null, settings.AdminUser.Password);
                var adminUser = new User
                {
                    Email = settings.AdminUser.Email,
                    FirstName = settings.AdminUser.FirstName,
                    LastName = settings.AdminUser.LastName,
                    PasswordHash = adminHashedPassword,
                    Role = "Admin",
                    IsActive = true,
                    TenantId = 1, // Default tenant
                    CreatedAt = DateTime.UtcNow
                };
                
                var demoHashedPassword = passwordHasher.HashPassword(null, "demo123");
                var demoUser = new User
                {
                    Email = "demo@nexora.com",
                    FirstName = "Demo",
                    LastName = "User",
                    PasswordHash = demoHashedPassword,
                    Role = "User",
                    IsActive = true,
                    TenantId = 1, // Default tenant
                    CreatedAt = DateTime.UtcNow
                };
                
                var testHashedPassword = passwordHasher.HashPassword(null, "TestPassword123!");
                var testUser = new User
                {
                    Email = "test@nexora.com",
                    FirstName = "Test",
                    LastName = "User",
                    PasswordHash = testHashedPassword,
                    Role = "User",
                    IsActive = true,
                    TenantId = 1, // Default tenant
                    CreatedAt = DateTime.UtcNow
                };
                
                await context.Users.AddRangeAsync(adminUser, demoUser, testUser);
                await context.SaveChangesAsync();
                
                logger.LogInformation("Seeded {UserCount} users successfully", 3);
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

