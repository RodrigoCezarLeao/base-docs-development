using DocMap.Application.Responses;
using DocMap.Application.Tracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMap.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/access")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AccessController(ITrackingService tracking) : ControllerBase
{
    /// <summary>Paginated access events (newest first), with filters.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await tracking.QueryAsync(new AccessQuery(userId, from, to, q, page, pageSize), cancellationToken);
        return Ok(ApiResponse<PagedResponse<AccessEventDto>>.Ok(result));
    }

    /// <summary>Admin erasure of a user's tracking data on request.</summary>
    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUserData(int userId, CancellationToken cancellationToken = default)
    {
        var deleted = await tracking.DeleteUserDataAsync(userId, cancellationToken);
        return Ok(ApiResponse<int>.Ok(deleted, $"{deleted} record(s) deleted."));
    }
}
