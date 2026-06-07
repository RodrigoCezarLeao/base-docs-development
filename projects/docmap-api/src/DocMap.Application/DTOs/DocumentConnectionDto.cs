namespace DocMap.Application.DTOs;

public record DocumentConnectionDto(
    int Id,
    int ProjectId,
    int SourceDocumentId,
    int TargetDocumentId,
    string? Label,
    DateTime CreatedAt);
