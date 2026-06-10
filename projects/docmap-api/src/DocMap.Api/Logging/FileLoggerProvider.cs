using System.Security.Claims;

namespace DocMap.Api.Logging;

/// <summary>
/// ILoggerProvider that writes every log entry as a delimited line to the daily file:
/// <c>timestamp | level | category | requestId | userId | message</c>
/// (message last so any embedded separator is safe; newlines/pipes are sanitized).
/// </summary>
public sealed class FileLoggerProvider(string directory, IHttpContextAccessor httpContextAccessor) : ILoggerProvider
{
    private readonly FileLogWriter _writer = new(directory);

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _writer, httpContextAccessor);

    public void Dispose() { }
}

internal sealed class FileLogger(string category, FileLogWriter writer, IHttpContextAccessor http) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var ctx = http.HttpContext;
        var requestId = ctx?.TraceIdentifier ?? "-";
        var userId = ctx?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "-";

        var message = formatter(state, exception);
        if (exception is not null)
            message += $" EXCEPTION {exception.GetType().Name}: {exception.Message}";
        message = message.Replace("\r", " ").Replace("\n", "\\n").Replace("|", "/");

        var line = string.Join(" | ",
            DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Level(logLevel),
            category,
            requestId,
            userId,
            message);

        writer.Write(line);
    }

    private static string Level(LogLevel l) => l switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRIT",
        _ => "NONE",
    };
}
