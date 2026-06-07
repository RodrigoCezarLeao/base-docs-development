using DocMap.Application.DTOs;
using DocMap.Application.Requests;
using DocMap.Application.Responses;

namespace DocMap.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> GetMeAsync(int userId, CancellationToken cancellationToken = default);
}
