using FluentAssertions;
using TemperatureApi.Infrastructure.Metrics;
using Xunit;

namespace TemperatureApi.Tests.Unit.Metrics;

public class MetricsCollectorTests
{
    private readonly MetricsCollector _sut = new();

    [Fact]
    public void Records_Totals_InFlight_Endpoints_And_ActiveUsers()
    {
        _sut.RecordStart("u1");
        _sut.RecordStart("u2");

        _sut.Snapshot().InFlight.Should().Be(2);

        _sut.RecordEnd("GET /a");
        _sut.RecordEnd("GET /a");

        var snap = _sut.Snapshot();
        snap.TotalRequests.Should().Be(2);
        snap.InFlight.Should().Be(0);
        snap.ActiveUsers.Should().Be(2);
        snap.Endpoints.Should().ContainSingle(e => e.Endpoint == "GET /a" && e.Count == 2);
        snap.Traffic.Should().HaveCount(60);
    }

    [Fact]
    public void Endpoints_Are_Sorted_By_Count_Descending()
    {
        _sut.RecordStart(null); _sut.RecordEnd("GET /a");
        _sut.RecordStart(null); _sut.RecordEnd("GET /b");
        _sut.RecordStart(null); _sut.RecordEnd("GET /b");

        var snap = _sut.Snapshot();
        snap.Endpoints[0].Endpoint.Should().Be("GET /b");
        snap.Endpoints[0].Count.Should().Be(2);
        snap.ActiveUsers.Should().Be(0); // null identities are not tracked
    }
}
