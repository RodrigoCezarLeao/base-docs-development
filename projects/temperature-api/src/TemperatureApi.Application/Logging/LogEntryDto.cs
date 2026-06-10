namespace TemperatureApi.Application.Logging;

public record LogEntryDto(
    string Timestamp,
    string Level,
    string Category,
    string RequestId,
    string UserId,
    string Message);
