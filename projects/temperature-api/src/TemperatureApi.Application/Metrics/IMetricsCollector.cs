namespace TemperatureApi.Application.Metrics;

/// <summary>
/// In-process request metrics. Fed by the metrics middleware on every request;
/// read by the admin metrics endpoint. In-memory, per-process (resets on restart).
/// </summary>
public interface IMetricsCollector
{
    /// <summary>Call at request start: total++, traffic bucket++, mark identity active, in-flight++.</summary>
    void RecordStart(string? identity);

    /// <summary>Call at request end: per-endpoint count++ (with last-called), in-flight--.</summary>
    void RecordEnd(string endpoint);

    MetricsSnapshot Snapshot();
}
