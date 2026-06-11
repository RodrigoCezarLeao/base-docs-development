using System.Collections.Concurrent;
using DocMap.Application.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace DocMap.Infrastructure.Caching;

/// <summary>
/// <see cref="ICacheService"/> backed by <see cref="IMemoryCache"/>. Tracks live keys so it
/// can support prefix invalidation (IMemoryCache has no native "remove by prefix").
/// </summary>
public sealed class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(key, out T? cached))
            return cached!;

        var value = await factory();

        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl };
        options.RegisterPostEvictionCallback((evictedKey, _, _, _) => _keys.TryRemove((string)evictedKey, out _));

        cache.Set(key, value, options);
        _keys.TryAdd(key, 0);
        return value;
    }

    public void Remove(string key)
    {
        cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void RemoveByPrefix(string prefix)
    {
        foreach (var key in _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            Remove(key);
    }
}
