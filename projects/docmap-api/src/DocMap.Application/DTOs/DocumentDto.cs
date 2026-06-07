namespace DocMap.Application.DTOs;

public record DocumentDto(
    int Id,
    int ProjectId,
    string Title,
    string FilePath,
    string Content,
    double CanvasX,
    double CanvasY,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
