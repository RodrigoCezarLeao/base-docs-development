using TemperatureApi.Domain.Models;

namespace TemperatureApi.Application.Interfaces;

public interface ITemperatureReadingRepository : IBaseRepository<TemperatureReading, int>
{
    Task<(IEnumerable<TemperatureReading> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
