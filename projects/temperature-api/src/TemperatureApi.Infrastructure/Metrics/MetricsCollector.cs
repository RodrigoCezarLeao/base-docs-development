using System.Collections.Concurrent;
using TemperatureApi.Application.Metrics;

namespace TemperatureApi.Infrastructure.Metrics;

/// <summary>Thread-safe in-memory <see cref="IMetricsCollector"/>. Singleton.</summary>
public sealed class MetricsCollector : IMetricsCollector
{
    private const int Window = 60; // seconds for both the active-user and traffic windows

    private long _total;
    private int _inFlight;
    private readonly ConcurrentDictionary<string, EndpointAcc> _endpoints = new();
    private readonly ConcurrentDictionary<string, long> _identities = new(); // identity -> last-seen unix seconds
    private readonly long[] _bucketSecond = new long[Window];
    private readonly long[] _bucketCount = new long[Window];
    private readonly object _bucketLock = new();

    public void RecordStart(string? identity)
    {
        Interlocked.Increment(ref _total);
        Interlocked.Increment(ref _inFlight);

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!string.IsNullOrEmpty(identity))
            _identities[identity] = now;

        var idx = (int)(now % Window);
        lock (_bucketLock)
        {
            if (_bucketSecond[idx] != now) { _bucketSecond[idx] = now; _bucketCount[idx] = 0; }
            _bucketCount[idx]++;
        }
    }

    public void RecordEnd(string endpoint)
    {
        Interlocked.Decrement(ref _inFlight);
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _endpoints.AddOrUpdate(
            endpoint,
            _ => new EndpointAcc(1, nowMs),
            (_, acc) => new EndpointAcc(acc.Count + 1, nowMs));
    }

    public MetricsSnapshot Snapshot()
    {
        var nowSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var cutoff = nowSec - Window;
        foreach (var kv in _identities)
            if (kv.Value < cutoff) _identities.TryRemove(kv.Key, out _);

        var endpoints = _endpoints
            .Select(kv => new EndpointMetric(kv.Key, kv.Value.Count, kv.Value.LastMs))
            .OrderByDescending(e => e.Count)
            .ToList();

        var traffic = new List<TrafficPoint>(Window);
        lock (_bucketLock)
        {
            for (var s = nowSec - Window + 1; s <= nowSec; s++)
            {
                var idx = (int)(((s % Window) + Window) % Window);
                var count = _bucketSecond[idx] == s ? _bucketCount[idx] : 0;
                traffic.Add(new TrafficPoint(s, count));
            }
        }

        return new MetricsSnapshot(
            _identities.Count,
            Volatile.Read(ref _inFlight),
            Interlocked.Read(ref _total),
            endpoints,
            traffic);
    }

    private readonly record struct EndpointAcc(long Count, long LastMs);
}
