using TemperatureApi.Application.Responses;

namespace TemperatureApi.Application.Tracking;

public interface ITrackingService
{
    Task RecordConsentAsync(int? userId, string decision, string? ip, CancellationToken cancellationToken = default);
    Task<PagedResponse<AccessEventDto>> QueryAsync(AccessQuery query, CancellationToken cancellationToken = default);

    /// <summary>Erases a user's access events (right to be forgotten). Returns the number deleted.</summary>
    Task<int> DeleteUserDataAsync(int userId, CancellationToken cancellationToken = default);
}
