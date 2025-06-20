using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Nexora.Core.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection AddNexoraTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = "Nexora.Platform";
        var serviceVersion = "2.0.0";
        
        services.AddSingleton<NexoraTelemetry>();
        
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                    ["service.instance.id"] = Environment.MachineName,
                    ["service.namespace"] = "nexora.fintech"
                }))
            .WithTracing(tracing => tracing
                .AddSource(NexoraTelemetry.ActivitySourceName)
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.request.body.size", request.ContentLength);
                        activity.SetTag("http.request.header.user_agent", request.Headers.UserAgent.ToString());
                    };
                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.SetTag("http.response.body.size", response.ContentLength);
                    };
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.SetDbStatementForStoredProcedure = true;
                    options.EnrichWithIDbCommand = (activity, command) =>
                    {
                        activity.SetTag("db.query.parameters", command.Parameters.Count);
                    };
                })


                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(configuration["Telemetry:OTLP:Endpoint"] ?? "http://localhost:4317");
                    options.Protocol = OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(metrics => metrics
                .AddMeter(NexoraTelemetry.MeterName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddPrometheusExporter());

        return services;
    }
}

public class NexoraTelemetry
{
    public const string ActivitySourceName = "Nexora.Platform";
    public const string MeterName = "Nexora.Platform";
    
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    
    public static readonly Counter<long> ApiCallsTotal = Meter.CreateCounter<long>(
        "nexora_api_calls_total",
        "Total number of API calls");
    
    public static readonly Counter<long> PaymentTransactionsTotal = Meter.CreateCounter<long>(
        "nexora_payment_transactions_total", 
        "Total number of payment transactions");
    
    public static readonly Counter<long> UserActivitiesTotal = Meter.CreateCounter<long>(
        "nexora_user_activities_total",
        "Total number of user activities");
    
    public static readonly Counter<long> FraudDetectionTotal = Meter.CreateCounter<long>(
        "nexora_fraud_detection_total",
        "Total number of fraud detection checks");
    
    public static readonly Histogram<double> ApiCallsDuration = Meter.CreateHistogram<double>(
        "nexora_api_calls_duration",
        "Duration of API calls in milliseconds");
    
    public static readonly Histogram<double> PaymentProcessingDuration = Meter.CreateHistogram<double>(
        "nexora_payment_processing_duration",
        "Duration of payment processing in milliseconds");
    
    public static readonly Histogram<double> DatabaseQueryDuration = Meter.CreateHistogram<double>(
        "nexora_database_query_duration",
        "Duration of database queries in milliseconds");
    
    public static readonly ObservableGauge<int> ActiveUsersGauge = Meter.CreateObservableGauge<int>(
        "nexora_active_users",
        () => GetActiveUsersCount(),
        "Number of currently active users");
    
    public static readonly ObservableGauge<decimal> TotalBalanceGauge = Meter.CreateObservableGauge<decimal>(
        "nexora_total_balance",
        () => GetTotalBalance(),
        "Total balance across all accounts");
    
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource.StartActivity(name, kind);
    }
    
    public static void RecordApiCall(string endpoint, string method, int statusCode, double duration)
    {
        var tags = new TagList();
        tags.Add("endpoint", endpoint ?? "unknown");
        tags.Add("method", method ?? "unknown");
        tags.Add("status_code", statusCode.ToString());
        tags.Add("status_class", GetStatusClass(statusCode));
        
        ApiCallsTotal.Add(1, tags);
        ApiCallsDuration.Record(duration, tags);
    }
    
    public static void RecordPaymentTransaction(string type, string status, decimal amount, double duration)
    {
        var tags = new TagList();
        tags.Add("type", type ?? "unknown");
        tags.Add("status", status ?? "unknown");
        tags.Add("amount_range", GetAmountRange(amount));
        
        PaymentTransactionsTotal.Add(1, tags);
        PaymentProcessingDuration.Record(duration, tags);
    }
    
    public static void RecordUserActivity(string userId, string activity, string tenantId)
    {
        var tags = new TagList();
        tags.Add("user_id", userId ?? "unknown");
        tags.Add("activity", activity ?? "unknown");
        tags.Add("tenant_id", tenantId ?? "unknown");
        
        UserActivitiesTotal.Add(1, tags);
    }
    
    public static void RecordFraudDetection(string transactionId, bool isFraud, double riskScore)
    {
        var tags = new TagList();
        tags.Add("transaction_id", transactionId ?? "unknown");
        tags.Add("is_fraud", isFraud.ToString());
        tags.Add("risk_level", GetRiskLevel(riskScore));
        
        FraudDetectionTotal.Add(1, tags);
    }
    
    private static string GetStatusClass(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "2xx",
            >= 300 and < 400 => "3xx",
            >= 400 and < 500 => "4xx",
            >= 500 => "5xx",
            _ => "unknown"
        };
    }
    
    private static string GetAmountRange(decimal amount)
    {
        return amount switch
        {
            < 100 => "small",
            < 1000 => "medium",
            < 10000 => "large",
            _ => "very_large"
        };
    }
    
    private static string GetRiskLevel(double riskScore)
    {
        return riskScore switch
        {
            < 0.3 => "low",
            < 0.7 => "medium",
            < 0.9 => "high",
            _ => "critical"
        };
    }
    
    private static int GetActiveUsersCount()
    {
        return 0;
    }
    
    private static decimal GetTotalBalance()
    {
        return 0m;
    }
}
