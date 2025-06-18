using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Nexora.Core.Caching;

public interface IAdvancedCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task RefreshAsync(string key, CancellationToken cancellationToken = default);
    Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class;
    Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
}

public class AdvancedCacheService : IAdvancedCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<AdvancedCacheService> _logger;
    private readonly CacheConfiguration _config;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, DateTime> _keyTracker = new();

    public AdvancedCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<AdvancedCacheService> logger,
        IConfiguration configuration)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _config = configuration.GetSection("Caching").Get<CacheConfiguration>() ?? new CacheConfiguration();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                _logger.LogDebug("Cache hit (L1) for key: {Key}", key);
                return cachedValue;
            }

            var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(distributedValue))
            {
                var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue);
                if (deserializedValue != null)
                {
                    var l1Options = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_config.L1ExpirationMinutes),
                        SlidingExpiration = TimeSpan.FromMinutes(_config.L1SlidingExpirationMinutes),
                        Priority = CacheItemPriority.Normal
                    };
                    _memoryCache.Set(key, deserializedValue, l1Options);
                    
                    _logger.LogDebug("Cache hit (L2) for key: {Key}", key);
                    return deserializedValue;
                }
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        try
        {
            var actualExpiration = expiration ?? TimeSpan.FromMinutes(_config.DefaultExpirationMinutes);
            
            var l1Options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Min(_config.L1ExpirationMinutes, (int)actualExpiration.TotalMinutes)),
                SlidingExpiration = TimeSpan.FromMinutes(_config.L1SlidingExpirationMinutes),
                Priority = CacheItemPriority.Normal
            };
            _memoryCache.Set(key, value, l1Options);

            var serializedValue = JsonSerializer.Serialize(value);
            var l2Options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = actualExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(_config.L2SlidingExpirationMinutes)
            };
            await _distributedCache.SetStringAsync(key, serializedValue, l2Options, cancellationToken);

            _keyTracker.TryAdd(key, DateTime.UtcNow);

            _logger.LogDebug("Cache set for key: {Key}, expiration: {Expiration}", key, actualExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            _memoryCache.Remove(key);

            await _distributedCache.RemoveAsync(key, cancellationToken);

            _keyTracker.TryRemove(key, out _);

            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        try
        {
            var keysToRemove = _keyTracker.Keys
                .Where(key => IsPatternMatch(key, pattern))
                .ToList();

            var removeTasks = keysToRemove.Select(key => RemoveAsync(key, cancellationToken));
            await Task.WhenAll(removeTasks);

            _logger.LogDebug("Cache removed by pattern: {Pattern}, keys removed: {Count}", pattern, keysToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
            throw;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        var lockKey = $"lock:{key}";
        var lockSemaphore = _locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        try
        {
            await lockSemaphore.WaitAsync(TimeSpan.FromSeconds(_config.LockTimeoutSeconds), cancellationToken);

            cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            _logger.LogDebug("Cache miss, generating value for key: {Key}", key);
            var value = await factory();

            if (value != null)
            {
                await SetAsync(key, value, expiration, cancellationToken);
            }

            return value;
        }
        finally
        {
            lockSemaphore.Release();
            
            if (_locks.Count > _config.MaxLocks)
            {
                CleanupOldLocks();
            }
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        try
        {
            if (_memoryCache.TryGetValue(key, out _))
            {
                return true;
            }

            var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(distributedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(distributedValue))
            {
                await _distributedCache.RefreshAsync(key, cancellationToken);
                _logger.LogDebug("Cache refreshed for key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache for key: {Key}", key);
            throw;
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));

        var result = new Dictionary<string, T?>();
        var keyList = keys.ToList();

        if (!keyList.Any())
            return result;

        try
        {
            var getTasks = keyList.Select(async key =>
            {
                var value = await GetAsync<T>(key, cancellationToken);
                return new KeyValuePair<string, T?>(key, value);
            });

            var results = await Task.WhenAll(getTasks);
            
            foreach (var kvp in results)
            {
                result[kvp.Key] = kvp.Value;
            }

            _logger.LogDebug("Retrieved {Count} keys from cache", keyList.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving multiple keys from cache");
            throw;
        }
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        if (!items.Any())
            return;

        try
        {
            var setTasks = items.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration, cancellationToken));
            await Task.WhenAll(setTasks);

            _logger.LogDebug("Set {Count} keys in cache", items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple keys in cache");
            throw;
        }
    }

    private bool IsPatternMatch(string key, string pattern)
    {
        if (pattern.Contains('*'))
        {
            var regexPattern = "^" + pattern.Replace("*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(key, regexPattern);
        }
        
        return key.Contains(pattern);
    }

    private void CleanupOldLocks()
    {
        try
        {
            var locksToRemove = _locks
                .Where(kvp => !kvp.Value.CurrentCount.Equals(1))
                .Take(_locks.Count / 2)
                .ToList();

            foreach (var lockToRemove in locksToRemove)
            {
                _locks.TryRemove(lockToRemove.Key, out _);
            }

            _logger.LogDebug("Cleaned up {Count} old locks", locksToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old locks");
        }
    }
}

public class CacheConfiguration
{
    public int DefaultExpirationMinutes { get; set; } = 30;
    public int L1ExpirationMinutes { get; set; } = 5;
    public int L1SlidingExpirationMinutes { get; set; } = 2;
    public int L2SlidingExpirationMinutes { get; set; } = 10;
    public int LockTimeoutSeconds { get; set; } = 30;
    public int MaxLocks { get; set; } = 1000;
}

public static class AdvancedCacheExtensions
{
    public static IServiceCollection AddAdvancedCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CacheConfiguration>(configuration.GetSection("Caching"));
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "Nexora";
        });
        services.AddScoped<IAdvancedCacheService, AdvancedCacheService>();
        
        return services;
    }
}
