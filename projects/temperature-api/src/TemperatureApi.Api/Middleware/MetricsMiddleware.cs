using System.Security.Claims;
using Microsoft.AspNetCore.Routing;
using TemperatureApi.Application.Metrics;

namespace TemperatureApi.Api.Middleware;

/// <summary>Feeds the in-process metrics collector on every request (in-flight, totals, endpoints, traffic).</summary>
public sealed class MetricsMiddleware(RequestDelegate next)
{
    // Don't measure the dashboard's own polling or the probe endpoints.
    private static readonly string[] Ignored = ["/api/v1/admin/metrics", "/health", "/ping", "/version"];

    public async Task InvokeAsync(HttpContext context, IMetricsCollector metrics)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (Ignored.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var identity = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString();

        metrics.RecordStart(identity);
        try
        {
            await next(context);
        }
        finally
        {
            // Route template (e.g. "api/v1/temperaturereadings/{id:int}") keeps cardinality low.
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? path;
            metrics.RecordEnd($"{context.Request.Method} {route}");
        }
    }
}
