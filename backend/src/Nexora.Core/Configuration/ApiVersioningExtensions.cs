using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Nexora.Core.Configuration;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddNexoraApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("version")
            );
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    public static IServiceCollection AddNexoraSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Nexora Platform API",
                Version = "v1.0",
                Description = "Enterprise-grade fintech platform API with comprehensive security and monitoring",
                Contact = new OpenApiContact
                {
                    Name = "Nexora Support",
                    Email = "support@nexora.com",
                    Url = new Uri("https://nexora.com/support")
                },
                License = new OpenApiLicense
                {
                    Name = "Proprietary License",
                    Url = new Uri("https://nexora.com/license")
                }
            });

            options.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = "Nexora Platform API",
                Version = "v2.0",
                Description = "Enhanced enterprise-grade fintech platform API with advanced features",
                Contact = new OpenApiContact
                {
                    Name = "Nexora Support",
                    Email = "support@nexora.com",
                    Url = new Uri("https://nexora.com/support")
                }
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key needed to access the endpoints. X-Api-Key: {key}",
                In = ParameterLocation.Header,
                Name = "X-Api-Key",
                Type = SecuritySchemeType.ApiKey
            });

            options.AddSecurityDefinition("TenantId", new OpenApiSecurityScheme
            {
                Description = "Tenant ID for multi-tenant operations. X-Tenant-Id: {tenantId}",
                In = ParameterLocation.Header,
                Name = "X-Tenant-Id",
                Type = SecuritySchemeType.ApiKey
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            options.OperationFilter<SwaggerOperationFilter>();
            
            options.OperationFilter<FileUploadOperationFilter>();
        });

        return services;
    }
}

public class SwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {

        foreach (var response in operation.Responses.Values)
        {
            response.Headers ??= new Dictionary<string, OpenApiHeader>();
            
            response.Headers["X-RateLimit-Limit"] = new OpenApiHeader
            {
                Description = "Request limit per hour",
                Schema = new OpenApiSchema { Type = "integer" }
            };
            
            response.Headers["X-RateLimit-Remaining"] = new OpenApiHeader
            {
                Description = "Remaining requests in current window",
                Schema = new OpenApiSchema { Type = "integer" }
            };
            
            response.Headers["X-Correlation-ID"] = new OpenApiHeader
            {
                Description = "Correlation ID for request tracing",
                Schema = new OpenApiSchema { Type = "string" }
            };
        }
    }
}

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) || 
                       p.ParameterType == typeof(IFormFileCollection) ||
                       p.ParameterType == typeof(IEnumerable<IFormFile>))
            .ToList();

        if (fileParams.Any())
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = fileParams.ToDictionary(
                                p => p.Name,
                                p => new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                })
                        }
                    }
                }
            };
        }
    }
}
