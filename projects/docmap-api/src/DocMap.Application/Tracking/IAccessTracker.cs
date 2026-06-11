using DocMap.Domain.Models;

namespace DocMap.Application.Tracking;

/// <summary>Non-blocking sink for access events (enqueues to a background writer).</summary>
public interface IAccessTracker
{
    void Track(AccessEvent accessEvent);
}
