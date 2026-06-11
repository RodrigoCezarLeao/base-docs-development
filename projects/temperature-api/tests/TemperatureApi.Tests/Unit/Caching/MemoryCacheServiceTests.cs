using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using TemperatureApi.Infrastructure.Caching;
using Xunit;

namespace TemperatureApi.Tests.Unit.Caching;

public class MemoryCacheServiceTests
{
    private readonly MemoryCacheService _sut = new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task GetOrCreateAsync_CachesAfterFirstCall()
    {
        var calls = 0;
        Task<int> Factory() { calls++; return Task.FromResult(42); }

        var first = await _sut.GetOrCreateAsync("k", Factory);
        var second = await _sut.GetOrCreateAsync("k", Factory);

        first.Should().Be(42);
        second.Should().Be(42);
        calls.Should().Be(1); // factory ran only on the miss
    }

    [Fact]
    public async Task Remove_ForcesReload()
    {
        var calls = 0;
        Task<int> Factory() { calls++; return Task.FromResult(calls); }

        await _sut.GetOrCreateAsync("k", Factory);
        _sut.Remove("k");
        var afterRemove = await _sut.GetOrCreateAsync("k", Factory);

        afterRemove.Should().Be(2);
        calls.Should().Be(2);
    }

    [Fact]
    public async Task RemoveByPrefix_EvictsMatchingKeys()
    {
        await _sut.GetOrCreateAsync("user:1", () => Task.FromResult("a"));
        await _sut.GetOrCreateAsync("user:2", () => Task.FromResult("b"));
        await _sut.GetOrCreateAsync("project:1", () => Task.FromResult("c"));

        _sut.RemoveByPrefix("user:");

        var userReloaded = 0;
        await _sut.GetOrCreateAsync("user:1", () => { userReloaded++; return Task.FromResult("a2"); });
        var projectReloaded = 0;
        await _sut.GetOrCreateAsync("project:1", () => { projectReloaded++; return Task.FromResult("c2"); });

        userReloaded.Should().Be(1);    // evicted → factory ran again
        projectReloaded.Should().Be(0); // untouched → still cached
    }
}
