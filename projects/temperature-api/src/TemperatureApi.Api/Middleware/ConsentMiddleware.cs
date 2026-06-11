namespace TemperatureApi.Api.Middleware;

/// <summary>
/// Reads the X-Tracking-Consent header (set by the consenting frontend) and stashes the result
/// on the request, so every observer (tracking, request log, metrics) can gate on it.
/// </summary>
public sealed class ConsentMiddleware(RequestDelegate next)
{
    public const string ItemKey = "TrackingConsent";

    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers["X-Tracking-Consent"].ToString();
        context.Items[ItemKey] = string.Equals(header, "granted", StringComparison.OrdinalIgnoreCase);
        await next(context);
    }
}

public static class HttpContextConsentExtensions
{
    public static bool HasTrackingConsent(this HttpContext context) =>
        context.Items.TryGetValue(ConsentMiddleware.ItemKey, out var value) && value is true;
}
