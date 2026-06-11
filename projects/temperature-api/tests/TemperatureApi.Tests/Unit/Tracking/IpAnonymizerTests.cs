using System.Net;
using FluentAssertions;
using TemperatureApi.Infrastructure.Tracking;
using Xunit;

namespace TemperatureApi.Tests.Unit.Tracking;

public class IpAnonymizerTests
{
    [Fact]
    public void Disabled_KeepsFullIp() =>
        new IpAnonymizer(false).Apply(IPAddress.Parse("187.59.12.34")).Should().Be("187.59.12.34");

    [Fact]
    public void Enabled_MasksLastIpv4Octet() =>
        new IpAnonymizer(true).Apply(IPAddress.Parse("187.59.12.34")).Should().Be("187.59.12.0");

    [Fact]
    public void Null_ReturnsUnknown() =>
        new IpAnonymizer(true).Apply(null).Should().Be("unknown");
}
