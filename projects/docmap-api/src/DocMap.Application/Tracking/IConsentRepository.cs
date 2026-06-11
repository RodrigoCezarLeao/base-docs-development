using DocMap.Domain.Models;

namespace DocMap.Application.Tracking;

public interface IConsentRepository
{
    Task InsertAsync(Consent consent, CancellationToken cancellationToken = default);
}
