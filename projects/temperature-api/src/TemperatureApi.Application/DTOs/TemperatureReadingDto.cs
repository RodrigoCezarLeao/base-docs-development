namespace TemperatureApi.Application.DTOs;

public record TemperatureReadingDto(
    int Id,
    string Location,
    decimal ValueCelsius,
    DateTime RecordedAt,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
