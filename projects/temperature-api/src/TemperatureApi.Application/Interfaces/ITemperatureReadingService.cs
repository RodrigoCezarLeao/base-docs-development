using TemperatureApi.Application.DTOs;
using TemperatureApi.Application.Requests;
using TemperatureApi.Application.Responses;

namespace TemperatureApi.Application.Interfaces;

public interface ITemperatureReadingService
{
    Task<ApiResponse<TemperatureReadingDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<TemperatureReadingDto>>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<TemperatureReadingDto>> CreateAsync(CreateTemperatureReadingRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<TemperatureReadingDto>> UpdateAsync(int id, UpdateTemperatureReadingRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
