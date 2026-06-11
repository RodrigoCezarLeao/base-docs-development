using TemperatureApi.Domain.Models;

namespace TemperatureApi.Application.Tracking;

public interface IConsentRepository
{
    Task InsertAsync(Consent consent, CancellationToken cancellationToken = default);
}
