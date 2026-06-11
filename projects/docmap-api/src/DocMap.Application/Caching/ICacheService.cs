namespace DocMap.Application.Caching;

/// <summary>
/// In-process cache for rarely-changing reads. Use cache-aside (GetOrCreateAsync) and
/// invalidate the affected key(s) on every write. See guidelines/csharp-api.md.
/// </summary>
public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
    void Remove(string key);
    void RemoveByPrefix(string prefix);
}
