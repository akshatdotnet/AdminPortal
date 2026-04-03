using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PMS.Application.Interfaces.Services;

namespace PMS.Application.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;

    // Track keys for prefix-based removal
    private readonly HashSet<string> _keys = new();
    //private readonly Lock _lock = new();
    private readonly object _lock = new();

    private static readonly TimeSpan DefaultAbsolute = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultSliding = TimeSpan.FromMinutes(2);

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiry = null,
        TimeSpan? slidingExpiry = null)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            _logger.LogDebug("Cache HIT: {Key}", key);
            return cached;
        }

        _logger.LogDebug("Cache MISS: {Key}", key);

        var value = await factory();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiry ?? DefaultAbsolute,
            SlidingExpiration = slidingExpiry ?? DefaultSliding,
            Priority = CacheItemPriority.Normal
        };

        // Register eviction callback to clean up key tracking
        options.RegisterPostEvictionCallback((k, _, _, _) =>
        {
            lock (_lock) { _keys.Remove(k.ToString()!); }
        });

        _cache.Set(key, value, options);

        lock (_lock) { _keys.Add(key); }

        return value;
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        lock (_lock) { _keys.Remove(key); }
    }

    public void RemoveByPrefix(string prefix)
    {
        List<string> toRemove;
        lock (_lock)
        {
            toRemove = _keys.Where(k => k.StartsWith(prefix)).ToList();
        }

        foreach (var key in toRemove)
        {
            _cache.Remove(key);
            lock (_lock) { _keys.Remove(key); }
        }

        _logger.LogDebug("Cache invalidated {Count} keys with prefix: {Prefix}",
            toRemove.Count, prefix);
    }
}