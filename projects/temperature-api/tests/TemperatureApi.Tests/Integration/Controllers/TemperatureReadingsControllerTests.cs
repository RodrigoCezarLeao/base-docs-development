using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using TemperatureApi.Application.Requests;
using TemperatureApi.Domain.Models;
using TemperatureApi.Infrastructure.Repositories.Interfaces;
using TemperatureApi.Tests.Integration;

namespace TemperatureApi.Tests.Integration.Controllers;

public class TemperatureReadingsControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ITemperatureReadingRepository _repository;

    public TemperatureReadingsControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
        _repository = factory.RepositoryMock;
    }

    // ── GET /api/v1/temperaturereadings ──────────────────────────────────────

    [Fact]
    public async Task GET_All_Returns200WithPagedData()
    {
        var readings = new List<TemperatureReading> { MakeReading(1, "SP", 28m) };
        _repository.GetPagedAsync(1, 10).Returns((readings.AsEnumerable(), 1));

        var response = await _client.GetAsync("/api/v1/temperaturereadings?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/v1/temperaturereadings/{id} ─────────────────────────────────

    [Fact]
    public async Task GET_ById_WhenExists_Returns200()
    {
        _repository.GetByIdAsync(1).Returns(MakeReading(1, "Brasília", 26m));

        var response = await _client.GetAsync("/api/v1/temperaturereadings/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_ById_WhenNotFound_Returns404()
    {
        _repository.GetByIdAsync(999).ReturnsNull();

        var response = await _client.GetAsync("/api/v1/temperaturereadings/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/v1/temperaturereadings ─────────────────────────────────────

    [Fact]
    public async Task POST_ValidRequest_Returns201WithLocation()
    {
        var reading = MakeReading(10, "Belém", 33m);
        _repository.CreateAsync(Arg.Any<TemperatureReading>()).Returns(10);

        var request = new CreateTemperatureReadingRequest("Belém", 33m, DateTime.UtcNow);
        var response = await _client.PostAsJsonAsync("/api/v1/temperaturereadings", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/10");
    }

    [Fact]
    public async Task POST_InvalidRequest_Returns400()
    {
        // Sem body — DataAnnotations rejeita
        var response = await _client.PostAsJsonAsync("/api/v1/temperaturereadings",
            new { Location = "", ValueCelsius = -999m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/v1/temperaturereadings/{id} ─────────────────────────────────

    [Fact]
    public async Task PUT_WhenExists_Returns200()
    {
        _repository.GetByIdAsync(1).Returns(MakeReading(1, "SP", 28m));
        _repository.UpdateAsync(Arg.Any<TemperatureReading>()).Returns(true);

        var request = new UpdateTemperatureReadingRequest("São Paulo", 30m, DateTime.UtcNow, true);
        var response = await _client.PutAsJsonAsync("/api/v1/temperaturereadings/1", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PUT_WhenNotFound_Returns404()
    {
        _repository.GetByIdAsync(999).ReturnsNull();

        var request = new UpdateTemperatureReadingRequest("X", 0m, DateTime.UtcNow, true);
        var response = await _client.PutAsJsonAsync("/api/v1/temperaturereadings/999", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/v1/temperaturereadings/{id} ───────────────────────────────

    [Fact]
    public async Task DELETE_WhenExists_Returns200()
    {
        _repository.GetByIdAsync(1).Returns(MakeReading(1, "RJ", 30m));
        _repository.DeleteAsync(1).Returns(true);

        var response = await _client.DeleteAsync("/api/v1/temperaturereadings/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DELETE_WhenNotFound_Returns404()
    {
        _repository.GetByIdAsync(999).ReturnsNull();

        var response = await _client.DeleteAsync("/api/v1/temperaturereadings/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
