using DocMap.Application.DTOs;
using DocMap.Application.Interfaces;
using DocMap.Application.Requests;
using DocMap.Application.Responses;
using DocMap.Domain.Models;
using DocMap.Application.Interfaces;

namespace DocMap.Application.Services;

public class ConnectionService(
    IDocumentConnectionRepository connectionRepository,
    IDocumentRepository documentRepository,
    IProjectRepository projectRepository) : IConnectionService
{
    public async Task<ApiResponse<IEnumerable<DocumentConnectionDto>>> GetAllAsync(int projectId, int userId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<IEnumerable<DocumentConnectionDto>>.Fail($"Project with id {projectId} not found.");

        var connections = await connectionRepository.GetAllByProjectIdAsync(projectId, cancellationToken);
        return ApiResponse<IEnumerable<DocumentConnectionDto>>.Ok(connections.Select(MapToDto));
    }

    public async Task<ApiResponse<DocumentConnectionDto>> CreateAsync(int projectId, CreateConnectionRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<DocumentConnectionDto>.Fail($"Project with id {projectId} not found.");

        var sourceDoc = await documentRepository.GetByIdAsync(request.SourceDocumentId, cancellationToken);
        if (sourceDoc is null || sourceDoc.ProjectId != projectId)
            return ApiResponse<DocumentConnectionDto>.Fail($"Source document with id {request.SourceDocumentId} not found.");

        var targetDoc = await documentRepository.GetByIdAsync(request.TargetDocumentId, cancellationToken);
        if (targetDoc is null || targetDoc.ProjectId != projectId)
            return ApiResponse<DocumentConnectionDto>.Fail($"Target document with id {request.TargetDocumentId} not found.");

        var connection = new DocumentConnection
        {
            ProjectId = projectId,
            SourceDocumentId = request.SourceDocumentId,
            TargetDocumentId = request.TargetDocumentId,
            Label = request.Label,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var id = await connectionRepository.CreateAsync(connection, cancellationToken);
        connection.Id = id;

        await UpdateSourceReferences(sourceDoc, targetDoc, cancellationToken);

        return ApiResponse<DocumentConnectionDto>.Created(MapToDto(connection));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int projectId, int userId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<bool>.Fail($"Project with id {projectId} not found.");

        var connection = await connectionRepository.GetByIdAsync(id, cancellationToken);
        if (connection is null || connection.ProjectId != projectId)
            return ApiResponse<bool>.Fail($"Connection with id {id} not found.");

        var sourceDoc = await documentRepository.GetByIdAsync(connection.SourceDocumentId, cancellationToken);
        var targetDoc = await documentRepository.GetByIdAsync(connection.TargetDocumentId, cancellationToken);

        await connectionRepository.DeleteAsync(id, cancellationToken);

        if (sourceDoc is not null && targetDoc is not null)
            await RemoveSourceReference(sourceDoc, targetDoc, cancellationToken);

        return ApiResponse<bool>.Ok(true);
    }

    private async Task UpdateSourceReferences(Document sourceDoc, Document targetDoc, CancellationToken cancellationToken)
    {
        var referenceLink = $"- [{targetDoc.Title}]({targetDoc.FilePath})";
        string newContent;

        if (sourceDoc.Content.Contains("## Referências"))
        {
            newContent = sourceDoc.Content + "\n" + referenceLink;
        }
        else
        {
            newContent = sourceDoc.Content + "\n\n## Referências\n\n" + referenceLink;
        }

        sourceDoc.Content = newContent;
        sourceDoc.UpdatedAt = DateTime.UtcNow;
        await documentRepository.UpdateAsync(sourceDoc, cancellationToken);
    }

    private async Task RemoveSourceReference(Document sourceDoc, Document targetDoc, CancellationToken cancellationToken)
    {
        var referenceLink = $"- [{targetDoc.Title}]({targetDoc.FilePath})";
        var content = sourceDoc.Content;

        if (!content.Contains(referenceLink))
            return;

        // Remove the reference line
        var lines = content.Split('\n').ToList();
        lines.RemoveAll(l => l.Trim() == referenceLink);

        var newContent = string.Join('\n', lines);

        // If the Referências section is now empty (no bullet lines follow it), remove the whole section
        var sectionLines = newContent.Split('\n').ToList();
        var headerIndex = sectionLines.FindIndex(l => l.Trim() == "## Referências");
        if (headerIndex >= 0)
        {
            var hasAnyBullet = false;
            for (int i = headerIndex + 1; i < sectionLines.Count; i++)
            {
                var line = sectionLines[i].Trim();
                if (line.StartsWith("## ") && line != "## Referências")
                    break;
                if (line.StartsWith("- "))
                {
                    hasAnyBullet = true;
                    break;
                }
            }

            if (!hasAnyBullet)
            {
                // Remove the section header and surrounding blank lines
                while (headerIndex > 0 && string.IsNullOrWhiteSpace(sectionLines[headerIndex - 1]))
                    sectionLines.RemoveAt(headerIndex - 1);

                headerIndex = sectionLines.FindIndex(l => l.Trim() == "## Referências");
                if (headerIndex >= 0)
                {
                    sectionLines.RemoveAt(headerIndex);
                    // Remove trailing blank lines after section removal
                    while (headerIndex < sectionLines.Count && string.IsNullOrWhiteSpace(sectionLines[headerIndex]))
                        sectionLines.RemoveAt(headerIndex);
                }

                newContent = string.Join('\n', sectionLines);
            }
        }

        sourceDoc.Content = newContent;
        sourceDoc.UpdatedAt = DateTime.UtcNow;
        await documentRepository.UpdateAsync(sourceDoc, cancellationToken);
    }

    private static DocumentConnectionDto MapToDto(DocumentConnection c) =>
        new(c.Id, c.ProjectId, c.SourceDocumentId, c.TargetDocumentId, c.Label, c.CreatedAt);
}
