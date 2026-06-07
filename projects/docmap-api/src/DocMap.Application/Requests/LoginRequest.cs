using System.ComponentModel.DataAnnotations;

namespace DocMap.Application.Requests;

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password);
