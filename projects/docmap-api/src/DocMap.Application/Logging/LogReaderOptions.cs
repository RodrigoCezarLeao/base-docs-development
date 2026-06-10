namespace DocMap.Application.Logging;

/// <summary>Resolved absolute directory where the daily log files live.</summary>
public sealed class LogReaderOptions
{
    public required string Directory { get; init; }
}
