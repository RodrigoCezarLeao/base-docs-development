using System.Diagnostics;

namespace DocMap.Api.Middleware;

/// <summary>Logs one structured line per HTTP request (method, path, status, elapsed).</summary>
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("DocMap.Api.HTTP");

    public async Task InvokeAsync(HttpContext context)
    {
        // LGPD: don't record access logs until the user has consented.
        if (!context.HasTrackingConsent())
        {
            await next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("{Method} {Path} -> {Status} ({Elapsed}ms)",
                context.Request.Method, context.Request.Path.Value,
                context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}
