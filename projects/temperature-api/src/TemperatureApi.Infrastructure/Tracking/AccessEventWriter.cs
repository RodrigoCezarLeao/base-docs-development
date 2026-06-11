using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TemperatureApi.Application.Tracking;
using TemperatureApi.Domain.Models;

namespace TemperatureApi.Infrastructure.Tracking;

/// <summary>Drains the access-event queue in batches and persists them off the request path.</summary>
public sealed class AccessEventWriter(
    AccessEventQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<AccessEventWriter> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var first in queue.Reader.ReadAllAsync(stoppingToken))
        {
            var batch = new List<AccessEvent> { first };
            while (batch.Count < 200 && queue.Reader.TryRead(out var next))
                batch.Add(next);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IAccessEventRepository>();
                await repository.InsertManyAsync(batch, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist {Count} access events", batch.Count);
            }
        }
    }
}
