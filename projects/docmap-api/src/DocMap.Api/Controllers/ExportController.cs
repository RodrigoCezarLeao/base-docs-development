using System.Security.Claims;
using DocMap.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMap.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:int}/export")]
[Authorize]
[Produces("application/json")]
public class ExportController(IDocumentService documentService, IProjectService projectService) : ControllerBase
{
    private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Export(int projectId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var projectResponse = await projectService.GetByIdAsync(projectId, userId, cancellationToken);
        if (!projectResponse.Success)
            return NotFound(projectResponse);

        var exportResponse = await documentService.ExportProjectAsync(projectId, userId, cancellationToken);
        if (!exportResponse.Success)
            return NotFound(exportResponse);

        var fileName = $"{projectResponse.Data!.Name.ToLower().Replace(" ", "-")}.zip";
        return File(exportResponse.Data!, "application/zip", fileName);
    }
}
