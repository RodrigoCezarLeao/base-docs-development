using System.Security.Claims;
using TemperatureApi.Application.Tracking;
using TemperatureApi.Domain.Models;
using TemperatureApi.Infrastructure.Tracking;
using UAParser;

namespace TemperatureApi.Api.Middleware;

/// <summary>
/// Records an access event per request — but ONLY when the user has consented (LGPD gate).
/// Enqueues to a background writer so the request path stays fast.
/// </summary>
public sealed class AccessTrackingMiddleware(RequestDelegate next)
{
    private static readonly Parser UaParser = Parser.GetDefault();
    private static readonly string[] Ignored = ["/api/v1/admin", "/api/v1/me/consent", "/health", "/ping", "/version"];

    public async Task InvokeAsync(HttpContext context, IAccessTracker tracker, IpAnonymizer ipAnonymizer)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!context.HasTrackingConsent() || Ignored.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        try
        {
            await next(context);
        }
        finally
        {
            var ua = context.Request.Headers.UserAgent.ToString();
            var client = string.IsNullOrEmpty(ua) ? null : UaParser.Parse(ua);
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            tracker.Track(new AccessEvent
            {
                UserId = int.TryParse(userIdClaim, out var id) ? id : null,
                Ip = ipAnonymizer.Apply(context.Connection.RemoteIpAddress),
                UserAgent = string.IsNullOrEmpty(ua) ? null : ua,
                Browser = client?.UA.Family,
                Os = client?.OS.Family,
                DeviceType = client?.Device.Family,
                Method = context.Request.Method,
                Path = path,
                StatusCode = context.Response.StatusCode,
                OccurredAt = DateTime.UtcNow,
            });
        }
    }
}
