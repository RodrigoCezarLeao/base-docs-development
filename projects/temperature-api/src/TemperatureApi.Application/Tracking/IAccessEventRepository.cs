using TemperatureApi.Application.Responses;
using TemperatureApi.Domain.Models;

namespace TemperatureApi.Application.Tracking;

public interface IAccessEventRepository
{
    Task InsertManyAsync(IReadOnlyList<AccessEvent> events, CancellationToken cancellationToken = default);
    Task<PagedResponse<AccessEventDto>> QueryAsync(AccessQuery query, CancellationToken cancellationToken = default);

    /// <summary>Right to erasure: hard-deletes a user's events (no soft-delete, no shadow copy).</summary>
    Task<int> DeleteByUserAsync(int userId, CancellationToken cancellationToken = default);
}
