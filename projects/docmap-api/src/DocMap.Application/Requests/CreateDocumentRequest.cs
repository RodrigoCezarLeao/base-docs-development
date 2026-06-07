using System.ComponentModel.DataAnnotations;

namespace DocMap.Application.Requests;

public record CreateDocumentRequest(
    [Required][MaxLength(200)] string Title,
    [Required] string FilePath,
    string Content = "",
    double CanvasX = 0,
    double CanvasY = 0);
