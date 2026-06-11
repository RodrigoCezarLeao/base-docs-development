namespace TemperatureApi.Application.Metrics;

public record MetricsSnapshot(
    int ActiveUsers,
    int InFlight,
    long TotalRequests,
    IReadOnlyList<EndpointMetric> Endpoints,
    IReadOnlyList<TrafficPoint> Traffic);

public record EndpointMetric(string Endpoint, long Count, long LastCalledUnixMs);

public record TrafficPoint(long UnixSeconds, long Count);
