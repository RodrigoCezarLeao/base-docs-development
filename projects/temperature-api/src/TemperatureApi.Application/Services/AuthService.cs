using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TemperatureApi.Application.DTOs;
using TemperatureApi.Application.Interfaces;
using TemperatureApi.Application.Requests;
using TemperatureApi.Application.Responses;
using TemperatureApi.Domain.Models;

namespace TemperatureApi.Application.Services;

public class AuthService(IUserRepository userRepository, IConfiguration configuration) : IAuthService
{
    private readonly string _jwtSecret = configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret configuration is missing.");

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            return ApiResponse<AuthResponse>.Fail("Email already in use.");

        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var id = await userRepository.CreateAsync(user, cancellationToken);
        user.Id = id;

        var token = GenerateToken(user);
        return ApiResponse<AuthResponse>.Created(new AuthResponse(token, MapToDto(user)));
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResponse<AuthResponse>.Fail("Invalid email or password.");

        var token = GenerateToken(user);
        return ApiResponse<AuthResponse>.Ok(new AuthResponse(token, MapToDto(user)));
    }

    public async Task<ApiResponse<UserDto>> GetMeAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return ApiResponse<UserDto>.Fail($"User with id {userId} not found.");

        return ApiResponse<UserDto>.Ok(MapToDto(user));
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name)
        };
        if (user.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapToDto(User u) => new(u.Id, u.Email, u.Name, u.IsAdmin);
}
