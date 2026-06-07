using System.ComponentModel.DataAnnotations;

namespace DocMap.Application.Requests;

public record UpdateDocumentRequest(
    [Required][MaxLength(200)] string Title,
    [Required] string FilePath,
    [Required] string Content);
