using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Net;

namespace Nexora.Core.Resilience;

public static class ResilienceExtensions
{
    public static IServiceCollection AddNexoraResilience(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddResilienceEnricher();
        
        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(options =>
            {
                options.Retry = new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(response => !response.IsSuccessStatusCode && 
                                                response.StatusCode != HttpStatusCode.NotFound)
                };
                
                options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 10,
                    BreakDuration = TimeSpan.FromSeconds(30),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(response => response.StatusCode >= HttpStatusCode.InternalServerError)
                };
                
                options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
            });
        });
        
        services.AddResiliencePipeline("database", builder =>
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<TimeoutException>()
                    .Handle<InvalidOperationException>(ex => ex.Message.Contains("timeout"))
            });
            
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.7,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(60),
                ShouldHandle = new PredicateBuilder()
                    .Handle<TimeoutException>()
                    .Handle<InvalidOperationException>()
            });
            
            builder.AddTimeout(TimeSpan.FromSeconds(10));
        });
        
        services.AddResiliencePipeline("external-service", builder =>
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutException>()
            });
            
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.6,
                SamplingDuration = TimeSpan.FromMinutes(1),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromMinutes(2),
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutException>()
            });
            
            builder.AddTimeout(TimeSpan.FromSeconds(30));
        });
        
        return services;
    }
}

public class DatabaseResilienceService
{
    private readonly ResiliencePipeline _pipeline;
    
    public DatabaseResilienceService(IServiceProvider serviceProvider)
    {
        _pipeline = ResiliencePipeline.Empty;
    }
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(async _ => await operation(), cancellationToken);
    }
    
    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await _pipeline.ExecuteAsync(async _ => await operation(), cancellationToken);
    }
}

public class ExternalServiceResilienceService
{
    private readonly ResiliencePipeline _pipeline;
    
    public ExternalServiceResilienceService(IServiceProvider serviceProvider)
    {
        _pipeline = ResiliencePipeline.Empty;
    }
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(async _ => await operation(), cancellationToken);
    }
    
    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await _pipeline.ExecuteAsync(async _ => await operation(), cancellationToken);
    }
}
