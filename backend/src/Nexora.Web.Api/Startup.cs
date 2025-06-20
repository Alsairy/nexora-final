using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Nexora.Core.Configuration;
using Nexora.Core.Data;
using Nexora.Core.Interfaces;
using Nexora.Core.Resilience;
using Nexora.Core.Telemetry;
using Nexora.Infrastructure.Data.Migrations;
using Nexora.Infrastructure.Repositories;
using Nexora.Infrastructure.Services;
using Nexora.Web.Api.Middleware;
using System;

namespace Nexora.Web.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add configuration
            services.Configure<KeyVaultSettings>(Configuration.GetSection("KeyVault"));
            services.Configure<SeedingSettings>(Configuration.GetSection("Seeding"));
            services.Configure<JwtSettings>(Configuration.GetSection("Jwt"));
            services.Configure<TenancySettings>(Configuration.GetSection("Tenancy"));

            services.AddNexoraTelemetry(Configuration);

            // Add resilience patterns
            services.AddNexoraResilience(Configuration);

            services.AddNexoraApiVersioning();

            services.AddNexoraHealthChecks(Configuration);

            // Add database - using SQLite for local development
            services.AddDbContext<NexoraDbContext>((provider, options) =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("Server=localhost"))
                {
                    options.UseSqlite("Data Source=nexora.db");
                }
                else
                {
                    options.UseSqlServer(connectionString,
                        sqlOptions =>
                        {
                            // Add resiliency with retry on failure
                            sqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(30),
                                errorNumbersToAdd: null);
                        });
                }
            });

            // Add Redis cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration.GetSection("Redis:ConnectionString").Value;
                options.InstanceName = Configuration.GetSection("Redis:InstanceName").Value;
            });

            // Add services
            services.AddScoped<Nexora.Core.Interfaces.ITenantService, TenantService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<IKeyVaultService, KeyVaultService>();
            services.AddScoped<ICryptoHelper, CryptoHelper>();
            services.AddScoped<Nexora.Core.Interfaces.IPaymentService, PaymentService>();
            // Temporarily disabled E-signature services due to DbContext issues
            // services.AddScoped<IESignatureService, ESignatureService>();
            // services.AddScoped<IESignatureComplianceService, ESignatureComplianceService>();
            // services.AddScoped<IESignatureTemplateService, ESignatureTemplateService>();
            // services.AddScoped<IESignatureWorkflowService, ESignatureWorkflowService>();
            // services.AddScoped<IESignatureAuthenticationService, ESignatureAuthenticationService>();
            // services.AddScoped<IESignatureNotificationService, ESignatureNotificationService>();

            // Add repositories
            services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();

            services.AddHttpContextAccessor();

            // Add controllers
            services.AddControllers();

            // Add authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidAudience = Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(Configuration["Jwt:SecretKey"] ?? Configuration["Jwt:Key"] ?? "default-development-secret-key-that-is-at-least-32-characters-long"))
                };
            });

            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("NexoraPolicy", builder =>
                {
                    builder.WithOrigins("https://app.nexora.com", "https://admin.nexora.com", "http://localhost:3000", "https://localhost:3000")
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
            });

            services.AddNexoraSwagger();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<RequestLoggingMiddleware>();

            // Add security headers middleware
            app.UseMiddleware<SecurityHeadersMiddleware>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nexora API V1");
                c.SwaggerEndpoint("/swagger/v2/swagger.json", "Nexora API V2");
                c.RoutePrefix = "swagger";
                c.DocumentTitle = "Nexora Platform API Documentation";
                c.DefaultModelExpandDepth(2);
                c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
                c.DisplayRequestDuration();
                c.EnableDeepLinking();
                c.EnableFilter();
                c.ShowExtensions();
            });

            // Add health checks endpoints
            app.UseHealthChecks("/health");
            app.UseHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("critical")
            });
            app.UseHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = _ => false
            });

            // Use HTTPS redirection
            app.UseHttpsRedirection();

            // Use routing
            app.UseRouting();

            // Use CORS
            app.UseCors("NexoraPolicy");

            // app.UseMiddleware<RateLimitingMiddleware>();

            // Use tenant middleware
            app.UseMiddleware<TenantMiddleware>();

            // Use authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Temporarily disable custom authorization middleware to fix login issues
            // app.UseMiddleware<AuthorizationMiddleware>();

            // Use exception handling middleware
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Use endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Initialize database and seed data
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var services = scope.ServiceProvider;
                SeedData.InitializeAsync(services).Wait();
            }
        }
    }
}

