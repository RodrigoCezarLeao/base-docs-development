namespace DocMap.Domain.Models;

public class DocumentConnection
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SourceDocumentId { get; set; }
    public int TargetDocumentId { get; set; }
    public string? Label { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
