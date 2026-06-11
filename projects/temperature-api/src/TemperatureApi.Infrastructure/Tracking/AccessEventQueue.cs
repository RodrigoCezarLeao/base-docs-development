using System.Threading.Channels;
using TemperatureApi.Application.Tracking;
using TemperatureApi.Domain.Models;

namespace TemperatureApi.Infrastructure.Tracking;

/// <summary>Singleton in-memory queue: the middleware enqueues, the background writer drains.</summary>
public sealed class AccessEventQueue : IAccessTracker
{
    private readonly Channel<AccessEvent> _channel = Channel.CreateBounded<AccessEvent>(
        new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.DropWrite });

    public ChannelReader<AccessEvent> Reader => _channel.Reader;

    public void Track(AccessEvent accessEvent) => _channel.Writer.TryWrite(accessEvent);
}
