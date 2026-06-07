using System.IO.Compression;
using System.Text;
using DocMap.Application.DTOs;
using DocMap.Application.Interfaces;
using DocMap.Application.Requests;
using DocMap.Application.Responses;
using DocMap.Domain.Models;
using DocMap.Application.Interfaces;

namespace DocMap.Application.Services;

public class DocumentService(IDocumentRepository documentRepository, IProjectRepository projectRepository) : IDocumentService
{
    public async Task<ApiResponse<IEnumerable<DocumentDto>>> GetAllAsync(int projectId, int userId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<IEnumerable<DocumentDto>>.Fail($"Project with id {projectId} not found.");

        var documents = await documentRepository.GetAllByProjectIdAsync(projectId, cancellationToken);
        return ApiResponse<IEnumerable<DocumentDto>>.Ok(documents.Select(MapToDto));
    }

    public async Task<ApiResponse<DocumentDto>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var document = await documentRepository.GetByIdAsync(id, cancellationToken);
        if (document is null)
            return ApiResponse<DocumentDto>.Fail($"Document with id {id} not found.");

        var project = await projectRepository.GetByIdAsync(document.ProjectId, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<DocumentDto>.Fail($"Document with id {id} not found.");

        return ApiResponse<DocumentDto>.Ok(MapToDto(document));
    }

    public async Task<ApiResponse<DocumentDto>> CreateAsync(int projectId, CreateDocumentRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<DocumentDto>.Fail($"Project with id {projectId} not found.");

        var document = new Document
        {
            ProjectId = projectId,
            Title = request.Title,
            FilePath = request.FilePath,
            Content = request.Content,
            CanvasX = request.CanvasX,
            CanvasY = request.CanvasY,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var id = await documentRepository.CreateAsync(document, cancellationToken);
        document.Id = id;

        return ApiResponse<DocumentDto>.Created(MapToDto(document));
    }

    public async Task<ApiResponse<DocumentDto>> UpdateAsync(int id, UpdateDocumentRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var document = await documentRepository.GetByIdAsync(id, cancellationToken);
        if (document is null)
            return ApiResponse<DocumentDto>.Fail($"Document with id {id} not found.");

        var project = await projectRepository.GetByIdAsync(document.ProjectId, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<DocumentDto>.Fail($"Document with id {id} not found.");

        document.Title = request.Title;
        document.FilePath = request.FilePath;
        document.Content = request.Content;
        document.UpdatedAt = DateTime.UtcNow;

        await documentRepository.UpdateAsync(document, cancellationToken);
        return ApiResponse<DocumentDto>.Ok(MapToDto(document));
    }

    public async Task<ApiResponse<DocumentDto>> UpdatePositionAsync(int id, UpdateDocumentPositionRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var document = await documentRepository.GetByIdAsync(id, cancellationToken);
        if (document is null)
            return ApiResponse<DocumentDto>.Fail($"Document with id {id} not found.");

        var project = await projectRepository.GetByIdAsync(document.ProjectId, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<DocumentDto>.Fail($"Document with id {id} not found.");

        await documentRepository.UpdatePositionAsync(id, request.CanvasX, request.CanvasY, cancellationToken);

        document.CanvasX = request.CanvasX;
        document.CanvasY = request.CanvasY;
        document.UpdatedAt = DateTime.UtcNow;

        return ApiResponse<DocumentDto>.Ok(MapToDto(document));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var document = await documentRepository.GetByIdAsync(id, cancellationToken);
        if (document is null)
            return ApiResponse<bool>.Fail($"Document with id {id} not found.");

        var project = await projectRepository.GetByIdAsync(document.ProjectId, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<bool>.Fail($"Document with id {id} not found.");

        await documentRepository.DeleteAsync(id, cancellationToken);
        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<byte[]>> ExportProjectAsync(int projectId, int userId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<byte[]>.Fail($"Project with id {projectId} not found.");

        var documents = await documentRepository.GetAllByProjectIdAsync(projectId, cancellationToken);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var document in documents)
            {
                var entry = archive.CreateEntry(document.FilePath, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                var contentBytes = Encoding.UTF8.GetBytes(document.Content);
                await entryStream.WriteAsync(contentBytes, cancellationToken);
            }
        }

        return ApiResponse<byte[]>.Ok(memoryStream.ToArray());
    }

    private static DocumentDto MapToDto(Document d) =>
        new(d.Id, d.ProjectId, d.Title, d.FilePath, d.Content, d.CanvasX, d.CanvasY, d.CreatedAt, d.UpdatedAt);
}
