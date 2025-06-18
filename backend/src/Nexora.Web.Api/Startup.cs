using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexora.Core.Configuration;
using Nexora.Core.Data;
using Nexora.Core.Interfaces;
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

            // Add database
            services.AddDbContext<NexoraDbContext>((provider, options) =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        // Add resiliency with retry on failure
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });
            });

            // Add Redis cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration.GetSection("Redis:ConnectionString").Value;
                options.InstanceName = Configuration.GetSection("Redis:InstanceName").Value;
            });

            // Add services
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<IKeyVaultService, KeyVaultService>();
            services.AddScoped<ICryptoHelper, CryptoHelper>();
            services.AddScoped<IPaymentService, PaymentService>();

            // Add repositories
            services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();

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
                        Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                };
            });

            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Add Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexora API", Version = "v1" });
                
                // Add JWT authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            // Use Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nexora API V1");
            });

            // Use HTTPS redirection
            app.UseHttpsRedirection();

            // Use routing
            app.UseRouting();

            // Use CORS
            app.UseCors("AllowAll");

            // Use tenant middleware
            app.UseMiddleware<TenantMiddleware>();

            // Use authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Use exception handling middleware
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Use endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Initialize database
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var services = scope.ServiceProvider;
                SeedData.InitializeAsync(services).Wait();
            }
        }
    }
}

