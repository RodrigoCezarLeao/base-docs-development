using TemperatureApi.Application.Caching;
using TemperatureApi.Application.DTOs;
using TemperatureApi.Application.Interfaces;
using TemperatureApi.Application.Requests;
using TemperatureApi.Application.Responses;
using TemperatureApi.Domain.Models;

namespace TemperatureApi.Application.Services;

public class TemperatureReadingService(ITemperatureReadingRepository repository, ICacheService cache) : ITemperatureReadingService
{
    public async Task<ApiResponse<TemperatureReadingDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Cache-aside: a reading rarely changes once recorded. Invalidated on update/delete.
        var reading = await cache.GetOrCreateAsync(
            CacheKeys.Reading(id),
            () => repository.GetByIdAsync(id, cancellationToken),
            cancellationToken: cancellationToken);

        if (reading is null)
            return ApiResponse<TemperatureReadingDto>.Fail($"Temperature reading with id {id} not found.");

        return ApiResponse<TemperatureReadingDto>.Ok(MapToDto(reading));
    }

    public async Task<ApiResponse<PagedResponse<TemperatureReadingDto>>> GetAllAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await repository.GetPagedAsync(page, pageSize, cancellationToken);
        var paged = new PagedResponse<TemperatureReadingDto>(items.Select(MapToDto), totalCount, page, pageSize);
        return ApiResponse<PagedResponse<TemperatureReadingDto>>.Ok(paged);
    }

    public async Task<ApiResponse<TemperatureReadingDto>> CreateAsync(
        CreateTemperatureReadingRequest request, CancellationToken cancellationToken = default)
    {
        var reading = new TemperatureReading
        {
            Location = request.Location,
            ValueCelsius = request.ValueCelsius,
            RecordedAt = request.RecordedAt,
            IsActive = true
        };

        var id = await repository.CreateAsync(reading, cancellationToken);
        reading.Id = id;
        return ApiResponse<TemperatureReadingDto>.Created(MapToDto(reading));
    }

    public async Task<ApiResponse<TemperatureReadingDto>> UpdateAsync(
        int id, UpdateTemperatureReadingRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return ApiResponse<TemperatureReadingDto>.Fail($"Temperature reading with id {id} not found.");

        existing.Location = request.Location;
        existing.ValueCelsius = request.ValueCelsius;
        existing.RecordedAt = request.RecordedAt;
        existing.IsActive = request.IsActive;

        await repository.UpdateAsync(existing, cancellationToken);
        cache.Remove(CacheKeys.Reading(id));
        return ApiResponse<TemperatureReadingDto>.Ok(MapToDto(existing), "Temperature reading updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return ApiResponse<bool>.Fail($"Temperature reading with id {id} not found.");

        await repository.DeleteAsync(id, cancellationToken);
        cache.Remove(CacheKeys.Reading(id));
        return ApiResponse<bool>.Ok(true, "Temperature reading deleted successfully.");
    }

    private static TemperatureReadingDto MapToDto(TemperatureReading r) =>
        new(r.Id, r.Location, r.ValueCelsius, r.RecordedAt, r.IsActive, r.CreatedAt, r.UpdatedAt);
}
