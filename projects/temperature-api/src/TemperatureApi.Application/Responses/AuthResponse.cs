using TemperatureApi.Application.DTOs;

namespace TemperatureApi.Application.Responses;

public record AuthResponse(string Token, UserDto User);
