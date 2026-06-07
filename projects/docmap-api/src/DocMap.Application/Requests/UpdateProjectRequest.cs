using System.ComponentModel.DataAnnotations;

namespace DocMap.Application.Requests;

public record UpdateProjectRequest(
    [Required][MaxLength(200)] string Name,
    string? Description);
