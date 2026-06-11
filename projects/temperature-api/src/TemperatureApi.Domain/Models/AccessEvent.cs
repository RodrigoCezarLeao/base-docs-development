namespace TemperatureApi.Domain.Models;

/// <summary>A tracked request — only ever recorded AFTER the user consents (see LGPD gate).</summary>
public class AccessEvent
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? DeviceType { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Country { get; set; } // reserved for future geo-IP enrichment
    public string? City { get; set; }
    public DateTime OccurredAt { get; set; }
}
