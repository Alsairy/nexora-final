using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Filters;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.Elasticsearch;
using System.Security.Claims;

namespace Nexora.Core.Telemetry;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddNexoraSerilog(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;
        
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Nexora.Platform")
            .Enrich.WithProperty("Environment", environment.EnvironmentName)
            .Enrich.WithProperty("Version", "2.0.0")
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithExceptionDetails()
            .Enrich.With<UserEnricher>()
            .Enrich.With<TenantEnricher>()
            .Enrich.With<CorrelationIdEnricher>()
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.StaticFiles"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics"))
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/nexora-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(configuration.GetConnectionString("Elasticsearch") ?? "http://localhost:9200"))
            {
                IndexFormat = "nexora-logs-{0:yyyy.MM.dd}",
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                CustomFormatter = new ElasticsearchJsonFormatter(),
                FailureCallback = e => Console.WriteLine($"Unable to submit event {e.MessageTemplate}"),
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                   EmitEventFailureHandling.WriteToFailureSink |
                                   EmitEventFailureHandling.RaiseCallback,
                FailureSink = new FileSink("logs/elasticsearch-failures-.log", new ElasticsearchJsonFormatter(), null)
            })
            .CreateLogger();

        builder.Host.UseSerilog();
        
        return builder;
    }
    
    public static WebApplication UseNexoraSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = (httpContext, elapsed, ex) => GetLogLevel(httpContext, elapsed, ex);
            options.EnrichDiagnosticContext = EnrichFromRequest;
        });
        
        return app;
    }
    
    private static LogEventLevel GetLogLevel(HttpContext ctx, double _, Exception? ex)
    {
        if (ex != null) return LogEventLevel.Error;
        
        return ctx.Response.StatusCode switch
        {
            >= 500 => LogEventLevel.Error,
            >= 400 => LogEventLevel.Warning,
            _ => LogEventLevel.Information
        };
    }
    
    private static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        var request = httpContext.Request;
        var response = httpContext.Response;
        var user = httpContext.User;
        
        diagnosticContext.Set("RequestHost", request.Host.Value);
        diagnosticContext.Set("RequestScheme", request.Scheme);
        diagnosticContext.Set("RequestProtocol", request.Protocol);
        diagnosticContext.Set("RequestContentType", request.ContentType);
        diagnosticContext.Set("RequestContentLength", request.ContentLength);
        diagnosticContext.Set("ResponseContentType", response.ContentType);
        diagnosticContext.Set("ResponseContentLength", response.ContentLength);
        
        if (user.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            diagnosticContext.Set("UserName", user.FindFirst(ClaimTypes.Name)?.Value);
            diagnosticContext.Set("UserEmail", user.FindFirst(ClaimTypes.Email)?.Value);
        }
        
        if (request.Headers.ContainsKey("X-Tenant-Id"))
        {
            diagnosticContext.Set("TenantId", request.Headers["X-Tenant-Id"].FirstOrDefault());
        }
        
        if (request.Headers.ContainsKey("X-Correlation-ID"))
        {
            diagnosticContext.Set("CorrelationId", request.Headers["X-Correlation-ID"].FirstOrDefault());
        }
        
        diagnosticContext.Set("UserAgent", request.Headers.UserAgent.FirstOrDefault());
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
    }
}

public class UserEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (LogContext.PushProperty("UserId", "system") is IDisposable)
        {
        }
    }
}

public class TenantEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (LogContext.PushProperty("TenantId", "default") is IDisposable)
        {
        }
    }
}

public class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
    }
}
