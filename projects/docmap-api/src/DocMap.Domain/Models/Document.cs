namespace DocMap.Domain.Models;

public class Document
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double CanvasX { get; set; }
    public double CanvasY { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
