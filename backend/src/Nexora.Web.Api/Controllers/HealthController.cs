using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nexora.Core.DTOs;
using System.Net;

namespace Nexora.Web.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    [MapToApiVersion("1.0")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(HealthCheckResponse), 200)]
    [ProducesResponseType(typeof(HealthCheckResponse), 503)]
    public async Task<IActionResult> Get()
    {
        var startTime = DateTime.UtcNow;
        var report = await _healthCheckService.CheckHealthAsync();
        var duration = DateTime.UtcNow - startTime;

        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            Duration = duration,
            Checks = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    Status = kvp.Value.Status.ToString(),
                    Description = kvp.Value.Description,
                    Duration = kvp.Value.Duration,
                    Exception = kvp.Value.Exception?.Message,
                    Data = kvp.Value.Data
                } as object)
        };

        var statusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
        return StatusCode(statusCode, response);
    }

    [HttpGet("detailed")]
    [MapToApiVersion("1.0")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetDetailed()
    {
        var startTime = DateTime.UtcNow;
        var report = await _healthCheckService.CheckHealthAsync();
        var duration = DateTime.UtcNow - startTime;

        var response = new
        {
            Status = report.Status.ToString(),
            TotalDuration = duration,
            Timestamp = DateTime.UtcNow,
            Checks = report.Entries.Select(kvp => new
            {
                Name = kvp.Key,
                Status = kvp.Value.Status.ToString(),
                Description = kvp.Value.Description,
                Duration = kvp.Value.Duration,
                Exception = kvp.Value.Exception?.Message,
                Data = kvp.Value.Data,
                Tags = kvp.Value.Tags
            }),
            Environment = new
            {
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet,
                Version = Environment.Version.ToString()
            }
        };

        return Ok(response);
    }

    [HttpGet("live")]
    [MapToApiVersion("1.0")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(200)]
    public IActionResult Live()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    [HttpGet("ready")]
    [MapToApiVersion("1.0")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> Ready()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        
        var criticalChecks = report.Entries
            .Where(kvp => kvp.Value.Tags.Contains("critical"))
            .ToList();

        var isReady = criticalChecks.All(kvp => kvp.Value.Status == HealthStatus.Healthy);
        
        var response = new
        {
            status = isReady ? "ready" : "not_ready",
            timestamp = DateTime.UtcNow,
            criticalChecks = criticalChecks.Select(kvp => new
            {
                name = kvp.Key,
                status = kvp.Value.Status.ToString(),
                description = kvp.Value.Description
            })
        };

        return isReady ? Ok(response) : StatusCode(503, response);
    }
}
