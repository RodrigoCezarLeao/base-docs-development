namespace TemperatureApi.Application.Tracking;

public record AccessEventDto(
    long Id,
    int? UserId,
    string Ip,
    string? Browser,
    string? Os,
    string? DeviceType,
    string Method,
    string Path,
    int StatusCode,
    string? Country,
    string? City,
    DateTime OccurredAt);

public record AccessQuery(
    int? UserId,
    DateTime? From,
    DateTime? To,
    string? Q,
    int Page,
    int PageSize);
