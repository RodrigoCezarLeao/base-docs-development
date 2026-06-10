using TemperatureApi.Application.DTOs;
using TemperatureApi.Application.Requests;
using TemperatureApi.Application.Responses;

namespace TemperatureApi.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> GetMeAsync(int userId, CancellationToken cancellationToken = default);
}
