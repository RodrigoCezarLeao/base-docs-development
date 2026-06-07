using System.ComponentModel.DataAnnotations;

namespace TemperatureApi.Application.Requests;

public record CreateTemperatureReadingRequest(
    [Required][MaxLength(100)] string Location,
    [Range(-100.0, 100.0, ErrorMessage = "ValueCelsius must be between -100 and 100.")] decimal ValueCelsius,
    [Required] DateTime RecordedAt
);
