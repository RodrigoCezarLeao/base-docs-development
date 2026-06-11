using DocMap.Application.Responses;
using DocMap.Application.Tracking;
using DocMap.Domain.Models;

namespace DocMap.Application.Services;

public class TrackingService(IAccessEventRepository accessEvents, IConsentRepository consents) : ITrackingService
{
    public Task RecordConsentAsync(int? userId, string decision, string? ip, CancellationToken cancellationToken = default) =>
        consents.InsertAsync(new Consent
        {
            UserId = userId,
            Decision = decision,
            PolicyVersion = PrivacyPolicy.Version,
            Ip = ip,
            OccurredAt = DateTime.UtcNow,
        }, cancellationToken);

    public Task<PagedResponse<AccessEventDto>> QueryAsync(AccessQuery query, CancellationToken cancellationToken = default) =>
        accessEvents.QueryAsync(query, cancellationToken);

    public Task<int> DeleteUserDataAsync(int userId, CancellationToken cancellationToken = default) =>
        accessEvents.DeleteByUserAsync(userId, cancellationToken);
}
