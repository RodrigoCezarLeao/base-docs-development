using DocMap.Application.DTOs;
using DocMap.Application.Interfaces;
using DocMap.Application.Requests;
using DocMap.Application.Responses;
using DocMap.Domain.Models;

namespace DocMap.Application.Services;

public class ProjectService(IProjectRepository projectRepository) : IProjectService
{
    public async Task<ApiResponse<IEnumerable<ProjectDto>>> GetAllAsync(int userId, CancellationToken cancellationToken = default)
    {
        var projects = await projectRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var dtos = new List<ProjectDto>();
        foreach (var project in projects)
        {
            var count = await projectRepository.GetDocumentCountAsync(project.Id, cancellationToken);
            dtos.Add(MapToDto(project, count));
        }
        return ApiResponse<IEnumerable<ProjectDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<ProjectDto>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(id, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<ProjectDto>.Fail($"Project with id {id} not found.");

        var count = await projectRepository.GetDocumentCountAsync(id, cancellationToken);
        return ApiResponse<ProjectDto>.Ok(MapToDto(project, count));
    }

    public async Task<ApiResponse<ProjectDto>> CreateAsync(CreateProjectRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var project = new Project
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var id = await projectRepository.CreateAsync(project, cancellationToken);
        project.Id = id;

        return ApiResponse<ProjectDto>.Created(MapToDto(project, 0));
    }

    public async Task<ApiResponse<ProjectDto>> UpdateAsync(int id, UpdateProjectRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(id, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<ProjectDto>.Fail($"Project with id {id} not found.");

        project.Name = request.Name;
        project.Description = request.Description;
        project.UpdatedAt = DateTime.UtcNow;

        await projectRepository.UpdateAsync(project, cancellationToken);

        var count = await projectRepository.GetDocumentCountAsync(id, cancellationToken);
        return ApiResponse<ProjectDto>.Ok(MapToDto(project, count));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(id, cancellationToken);
        if (project is null || project.UserId != userId)
            return ApiResponse<bool>.Fail($"Project with id {id} not found.");

        await projectRepository.DeleteAsync(id, cancellationToken);
        return ApiResponse<bool>.Ok(true);
    }

    private static ProjectDto MapToDto(Project p, int documentCount) =>
        new(p.Id, p.Name, p.Description, p.CreatedAt, documentCount);
}
