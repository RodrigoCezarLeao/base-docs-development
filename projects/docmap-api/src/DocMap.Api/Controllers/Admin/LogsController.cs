using DocMap.Application.Logging;
using DocMap.Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMap.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/logs")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class LogsController(ILogReader reader) : ControllerBase
{
    /// <summary>Available log dates (newest first).</summary>
    [HttpGet("files")]
    public IActionResult Files() =>
        Ok(ApiResponse<IEnumerable<string>>.Ok(reader.GetAvailableDates()));

    /// <summary>Parsed, filtered and paginated entries for a day (defaults to today).</summary>
    [HttpGet]
    public IActionResult Get(
        [FromQuery] string? date,
        [FromQuery] string? level,
        [FromQuery] string? category,
        [FromQuery] string? userId,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = reader.Query(new LogQuery(date, level, category, userId, q, page, pageSize));
        return Ok(ApiResponse<PagedResponse<LogEntryDto>>.Ok(result));
    }
}
