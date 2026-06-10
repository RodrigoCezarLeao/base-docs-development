using System.ComponentModel.DataAnnotations;

namespace TemperatureApi.Application.Requests;

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password);
