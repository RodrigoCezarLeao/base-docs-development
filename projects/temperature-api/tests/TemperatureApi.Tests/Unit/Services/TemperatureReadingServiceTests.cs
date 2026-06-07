using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using TemperatureApi.Application.Interfaces;
using TemperatureApi.Application.Requests;
using TemperatureApi.Application.Services;
using TemperatureApi.Domain.Models;
using Xunit;

namespace TemperatureApi.Tests.Unit.Services;

public class TemperatureReadingServiceTests
{
    private readonly ITemperatureReadingRepository _repository =
        Substitute.For<ITemperatureReadingRepository>();

    private readonly TemperatureReadingService _sut;

    public TemperatureReadingServiceTests()
    {
        _sut = new TemperatureReadingService(_repository);
    }

    // ── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenReadingExists_ReturnsSuccess()
    {
        var reading = MakeReading(id: 1, location: "São Paulo", value: 28.5m);
        _repository.GetByIdAsync(1).Returns(reading);

        var result = await _sut.GetByIdAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(1);
        result.Data.Location.Should().Be("São Paulo");
        result.Data.ValueCelsius.Should().Be(28.5m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenReadingNotFound_ReturnsFailWithMessage()
    {
        _repository.GetByIdAsync(999).ReturnsNull();

        var result = await _sut.GetByIdAsync(999);

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Contain("999");
    }

    // ── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResponseWithItems()
    {
        var readings = new List<TemperatureReading>
        {
            MakeReading(1, "Campinas", 22m),
            MakeReading(2, "Curitiba", 15m),
        };
        _repository.GetPagedAsync(1, 10).Returns((readings.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
        result.Data.Page.Should().Be(1);
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedWithGeneratedId()
    {
        var request = new CreateTemperatureReadingRequest("Manaus", 35.0m, DateTime.UtcNow);
        _repository.CreateAsync(Arg.Any<TemperatureReading>()).Returns(42);

        var result = await _sut.CreateAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(42);
        result.Data.Location.Should().Be("Manaus");
        result.Data.ValueCelsius.Should().Be(35.0m);
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryWithMappedEntity()
    {
        var request = new CreateTemperatureReadingRequest("Recife", 30.0m, DateTime.UtcNow);
        _repository.CreateAsync(Arg.Any<TemperatureReading>()).Returns(1);

        await _sut.CreateAsync(request);

        await _repository.Received(1).CreateAsync(
            Arg.Is<TemperatureReading>(r =>
                r.Location == "Recife" &&
                r.ValueCelsius == 30.0m &&
                r.IsActive == true));
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenReadingExists_ReturnsSuccess()
    {
        var existing = MakeReading(1, "Porto Alegre", 18m);
        _repository.GetByIdAsync(1).Returns(existing);
        _repository.UpdateAsync(Arg.Any<TemperatureReading>()).Returns(true);

        var request = new UpdateTemperatureReadingRequest("Porto Alegre", 20m, DateTime.UtcNow, true);
        var result = await _sut.UpdateAsync(1, request);

        result.Success.Should().BeTrue();
        result.Data!.ValueCelsius.Should().Be(20m);
    }

    [Fact]
    public async Task UpdateAsync_WhenReadingNotFound_ReturnsFailWithMessage()
    {
        _repository.GetByIdAsync(404).ReturnsNull();

        var request = new UpdateTemperatureReadingRequest("X", 0m, DateTime.UtcNow, true);
        var result = await _sut.UpdateAsync(404, request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("404");
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<TemperatureReading>());
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenReadingExists_ReturnsSuccess()
    {
        _repository.GetByIdAsync(1).Returns(MakeReading(1, "Fortaleza", 32m));
        _repository.DeleteAsync(1).Returns(true);

        var result = await _sut.DeleteAsync(1);

        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenReadingNotFound_ReturnsFailWithoutCallingDelete()
    {
        _repository.GetByIdAsync(999).ReturnsNull();

        var result = await _sut.DeleteAsync(999);

        result.Success.Should().BeFalse();
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<int>());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static TemperatureReading MakeReading(int id, string location, decimal value) => new()
    {
        Id = id,
        Location = location,
        ValueCelsius = value,
        RecordedAt = DateTime.UtcNow,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
    };
}
