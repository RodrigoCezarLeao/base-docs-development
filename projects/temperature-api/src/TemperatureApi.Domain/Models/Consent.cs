namespace TemperatureApi.Domain.Models;

/// <summary>Proof-of-consent audit record (granted / withdrawn / denied). Not behavioral tracking.</summary>
public class Consent
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string Decision { get; set; } = string.Empty; // granted | withdrawn | denied
    public string PolicyVersion { get; set; } = string.Empty;
    public string? Ip { get; set; }
    public DateTime OccurredAt { get; set; }
}
