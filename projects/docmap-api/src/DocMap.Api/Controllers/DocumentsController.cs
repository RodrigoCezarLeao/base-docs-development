using System.Security.Claims;
using DocMap.Application.Interfaces;
using DocMap.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMap.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:int}/documents")]
[Authorize]
[Produces("application/json")]
public class DocumentsController(IDocumentService documentService) : ControllerBase
{
    private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(int projectId, CancellationToken cancellationToken = default)
    {
        var response = await documentService.GetAllAsync(projectId, GetCurrentUserId(), cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        int projectId,
        [FromBody] CreateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await documentService.CreateAsync(projectId, request, GetCurrentUserId(), cancellationToken);
        if (!response.Success)
            return NotFound(response);
        return CreatedAtAction(nameof(GetById), new { projectId, id = response.Data!.Id }, response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int projectId, int id, CancellationToken cancellationToken = default)
    {
        var response = await documentService.GetByIdAsync(id, GetCurrentUserId(), cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        int projectId,
        int id,
        [FromBody] UpdateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await documentService.UpdateAsync(id, request, GetCurrentUserId(), cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPatch("{id:int}/position")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePosition(
        int projectId,
        int id,
        [FromBody] UpdateDocumentPositionRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await documentService.UpdatePositionAsync(id, request, GetCurrentUserId(), cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int projectId, int id, CancellationToken cancellationToken = default)
    {
        var response = await documentService.DeleteAsync(id, GetCurrentUserId(), cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }
}
