using DocMap.Application.Metrics;
using DocMap.Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMap.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/metrics")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class MetricsController(IMetricsCollector metrics) : ControllerBase
{
    /// <summary>Real-time snapshot: active users, in-flight, totals, per-endpoint counts, traffic.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(ApiResponse<MetricsSnapshot>.Ok(metrics.Snapshot()));
}
