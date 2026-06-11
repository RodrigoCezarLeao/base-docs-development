using FluentAssertions;
using TemperatureApi.Application.Logging;
using Xunit;

namespace TemperatureApi.Tests.Unit.Logging;

public class LogReaderServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly LogReaderService _sut;

    public LogReaderServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "logtest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _sut = new LogReaderService(new LogReaderOptions { Directory = _dir });
    }

    private void WriteDay(string date, params string[] lines) =>
        File.WriteAllLines(Path.Combine(_dir, $"{date}.txt"), lines);

    [Fact]
    public void Query_ReturnsEntries_NewestFirst()
    {
        WriteDay("2026-06-10",
            "2026-06-10T10:00:00.000Z | INFO | App | r1 | 1 | first",
            "2026-06-10T10:00:01.000Z | INFO | App | r2 | 1 | second");

        var result = _sut.Query(new LogQuery("2026-06-10", null, null, null, null, 1, 50));

        result.TotalCount.Should().Be(2);
        result.Items.First().Message.Should().Be("second"); // newest first
    }

    [Fact]
    public void Query_FiltersByLevelAndSearch()
    {
        WriteDay("2026-06-10",
            "2026-06-10T10:00:00.000Z | INFO | App | r1 | 1 | hello world",
            "2026-06-10T10:00:01.000Z | ERROR | App | r2 | 1 | boom");

        _sut.Query(new LogQuery("2026-06-10", "ERROR", null, null, null, 1, 50))
            .Items.Single().Message.Should().Be("boom");

        _sut.Query(new LogQuery("2026-06-10", null, null, null, "world", 1, 50))
            .TotalCount.Should().Be(1);
    }

    [Fact]
    public void Query_RejectsInvalidDate_GuardsAgainstPathTraversal()
    {
        var result = _sut.Query(new LogQuery("../secret", null, null, null, null, 1, 50));

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void GetAvailableDates_ReturnsDatesNewestFirst()
    {
        WriteDay("2026-06-09", "x");
        WriteDay("2026-06-11", "x");
        WriteDay("2026-06-10", "x");

        _sut.GetAvailableDates().Should().Equal("2026-06-11", "2026-06-10", "2026-06-09");
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }
}
