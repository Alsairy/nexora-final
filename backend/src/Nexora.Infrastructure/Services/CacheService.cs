using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(30); // Default TTL of 30 minutes

        public CacheService(IDistributedCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var data = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(data))
            {
                return default;
            }
            
            return JsonConvert.DeserializeObject<T>(data);
        }

        public async Task SetAsync<T>(string key, T value)
        {
            // Use default TTL of 30 minutes
            await SetAsync(key, value, _defaultTtl);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };
            
            var data = JsonConvert.SerializeObject(value);
            await _cache.SetStringAsync(key, data, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
        {
            // Use default TTL of 30 minutes
            return await GetOrCreateAsync(key, factory, _defaultTtl);
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
        {
            var cachedValue = await GetAsync<T>(key);
            
            if (cachedValue != null)
            {
                return cachedValue;
            }
            
            var value = await factory();
            
            if (value != null)
            {
                await SetAsync(key, value, ttl);
            }
            
            return value;
        }
    }

    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value);
        Task SetAsync<T>(string key, T value, TimeSpan ttl);
        Task RemoveAsync(string key);
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory);
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl);
    }
}

