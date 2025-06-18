using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Core.Telemetry;
using System.Diagnostics.Metrics;
using System.Text;

namespace Nexora.Web.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public class MetricsController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(string), 200)]
    public IActionResult GetMetrics()
    {
        var metrics = new StringBuilder();
        
        metrics.AppendLine("# HELP nexora_info Application information");
        metrics.AppendLine("# TYPE nexora_info gauge");
        metrics.AppendLine($"nexora_info{{version=\"2.0.0\",environment=\"{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}\"}} 1");
        
        metrics.AppendLine("# HELP nexora_uptime_seconds Application uptime in seconds");
        metrics.AppendLine("# TYPE nexora_uptime_seconds counter");
        metrics.AppendLine($"nexora_uptime_seconds {Environment.TickCount64 / 1000}");
        
        metrics.AppendLine("# HELP nexora_memory_working_set_bytes Current working set in bytes");
        metrics.AppendLine("# TYPE nexora_memory_working_set_bytes gauge");
        metrics.AppendLine($"nexora_memory_working_set_bytes {Environment.WorkingSet}");
        
        return Content(metrics.ToString(), "text/plain; version=0.0.4");
    }

    [HttpGet("json")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetMetricsJson()
    {
        var metrics = new
        {
            Application = new
            {
                Name = "Nexora Platform",
                Version = "2.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                UptimeSeconds = Environment.TickCount64 / 1000,
                StartTime = DateTime.UtcNow.AddMilliseconds(-Environment.TickCount64)
            },
            System = new
            {
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                WorkingSetBytes = Environment.WorkingSet,
                GCMemoryBytes = GC.GetTotalMemory(false),
                ThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count
            },
            GarbageCollection = new
            {
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TotalMemoryBytes = GC.GetTotalMemory(false)
            },
            Timestamp = DateTime.UtcNow
        };

        return Ok(metrics);
    }
}
