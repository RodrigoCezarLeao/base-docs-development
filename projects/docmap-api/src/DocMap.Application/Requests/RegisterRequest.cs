using System.ComponentModel.DataAnnotations;

namespace DocMap.Application.Requests;

public record RegisterRequest(
    [Required][MaxLength(100)] string Name,
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password);
