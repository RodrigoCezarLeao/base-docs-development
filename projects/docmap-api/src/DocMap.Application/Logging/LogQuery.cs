namespace DocMap.Application.Logging;

/// <summary>Pre-mapped filters for the admin log viewer (all optional except paging defaults).</summary>
public record LogQuery(
    string? Date,
    string? Level,
    string? Category,
    string? UserId,
    string? Q,
    int Page,
    int PageSize);
