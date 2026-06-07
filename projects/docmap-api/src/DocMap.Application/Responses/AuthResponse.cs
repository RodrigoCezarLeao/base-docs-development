using DocMap.Application.DTOs;

namespace DocMap.Application.Responses;

public record AuthResponse(string Token, UserDto User);
