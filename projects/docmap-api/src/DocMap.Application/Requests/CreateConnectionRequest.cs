using System.ComponentModel.DataAnnotations;

namespace DocMap.Application.Requests;

public record CreateConnectionRequest(
    [Required] int SourceDocumentId,
    [Required] int TargetDocumentId,
    string? Label);
