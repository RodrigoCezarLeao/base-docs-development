using TemperatureApi.Application.Caching;

namespace TemperatureApi.Tests.Fakes;

/// <summary>Test double that never caches — always runs the factory. Keeps service tests deterministic.</summary>
public sealed class PassThroughCache : ICacheService
{
    public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default) => factory();
    public void Remove(string key) { }
    public void RemoveByPrefix(string prefix) { }
}
