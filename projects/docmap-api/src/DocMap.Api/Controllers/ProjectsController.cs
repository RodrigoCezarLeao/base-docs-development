using System.Security.Claims;
using DocMap.Application.Interfaces;
using DocMap.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMap.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
[Produces("application/json")]
public class ProjectsController(IProjectService projectService) : ControllerBase
{
    private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var response = await projectService.GetAllAsync(GetCurrentUserId(), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await projectService.CreateAsync(request, GetCurrentUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Data!.Id }, response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var response = await projectService.GetByIdAsync(id, GetCurrentUserId(), cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await projectService.UpdateAsync(id, request, GetCurrentUserId(), cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var response = await projectService.DeleteAsync(id, GetCurrentUserId(), cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }
}
