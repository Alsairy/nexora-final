using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Core.DTOs;

namespace Nexora.Web.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}")]
[AllowAnonymous]
public class ApiController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult GetApiInfo()
    {
        var apiInfo = new
        {
            Name = "Nexora Platform API",
            Description = "Enterprise-grade fintech platform API with comprehensive security and monitoring",
            Version = "2.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            Timestamp = DateTime.UtcNow,
            SupportedVersions = new[] { "1.0", "2.0" },
            Documentation = new
            {
                SwaggerUI = "/swagger",
                OpenAPISpec = "/swagger/v2/swagger.json"
            },
            Endpoints = new
            {
                Health = "/health",
                Metrics = "/api/v2/metrics",
                Authentication = "/api/v2/auth",
                Users = "/api/v2/users",
                Payments = "/api/v2/payments",
                Transactions = "/api/v2/transactions",
                Tenants = "/api/v2/tenants"
            },
            Features = new[]
            {
                "Multi-tenant architecture",
                "JWT authentication",
                "Role-based authorization",
                "Rate limiting",
                "Comprehensive logging",
                "Health monitoring",
                "API versioning",
                "OpenAPI documentation",
                "Resilience patterns",
                "Security headers"
            }
        };

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = apiInfo,
            Message = "API information retrieved successfully"
        });
    }

    [HttpGet("status")]
    [MapToApiVersion("1.0")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult GetStatus()
    {
        var status = new
        {
            Status = "Operational",
            Timestamp = DateTime.UtcNow,
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            Version = "2.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        };

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = status,
            Message = "API is operational"
        });
    }
}
