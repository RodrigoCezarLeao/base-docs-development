using System.Security.Claims;
using DocMap.Api.Middleware;
using DocMap.Application.Responses;
using DocMap.Application.Tracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMap.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Produces("application/json")]
public class MeController(ITrackingService tracking) : ControllerBase
{
    public record ConsentRequest(string Decision);

    /// <summary>Records the user's privacy decision (granted | denied | withdrawn). Anonymous allowed.</summary>
    [HttpPost("consent")]
    public async Task<IActionResult> Consent([FromBody] ConsentRequest request, CancellationToken cancellationToken = default)
    {
        var decision = request.Decision?.ToLowerInvariant();
        if (decision is not ("granted" or "denied" or "withdrawn"))
            return BadRequest(ApiResponse<object>.Fail("Invalid decision."));

        var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
        var ip = HttpContext.HasTrackingConsent() ? HttpContext.Connection.RemoteIpAddress?.ToString() : null;

        await tracking.RecordConsentAsync(userId, decision, ip, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { decision }));
    }

    /// <summary>Right to erasure: hard-deletes the caller's access events (no backup kept).</summary>
    [HttpDelete("tracking-data")]
    [Authorize]
    public async Task<IActionResult> DeleteMyData(CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var deleted = await tracking.DeleteUserDataAsync(userId, cancellationToken);
        return Ok(ApiResponse<int>.Ok(deleted, $"{deleted} record(s) deleted."));
    }
}
