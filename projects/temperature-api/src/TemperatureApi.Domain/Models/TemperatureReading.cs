namespace TemperatureApi.Domain.Models;

public class TemperatureReading
{
    public int Id { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal ValueCelsius { get; set; }
    public DateTime RecordedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
