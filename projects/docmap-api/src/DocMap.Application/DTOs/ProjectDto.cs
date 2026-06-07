namespace DocMap.Application.DTOs;

public record ProjectDto(int Id, string Name, string? Description, DateTime CreatedAt, int DocumentCount);
