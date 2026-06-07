using System.Security.Claims;
using DocMap.Application.Interfaces;
using DocMap.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMap.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:int}/connections")]
[Authorize]
[Produces("application/json")]
public class ConnectionsController(IConnectionService connectionService) : ControllerBase
{
    private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(int projectId, CancellationToken cancellationToken = default)
    {
        var response = await connectionService.GetAllAsync(projectId, GetCurrentUserId(), cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        int projectId,
        [FromBody] CreateConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await connectionService.CreateAsync(projectId, request, GetCurrentUserId(), cancellationToken);
        return response.Success
            ? StatusCode(StatusCodes.Status201Created, response)
            : NotFound(response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int projectId, int id, CancellationToken cancellationToken = default)
    {
        var response = await connectionService.DeleteAsync(id, projectId, GetCurrentUserId(), cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }
}
