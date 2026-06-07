using TemperatureApi.Domain.Models;
using TemperatureApi.Infrastructure.Repositories.Base;

namespace TemperatureApi.Infrastructure.Repositories.Interfaces;

public interface ITemperatureReadingRepository : IBaseRepository<TemperatureReading, int>
{
    Task<(IEnumerable<TemperatureReading> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
