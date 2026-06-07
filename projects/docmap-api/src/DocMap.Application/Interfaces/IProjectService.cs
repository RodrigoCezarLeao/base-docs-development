using DocMap.Application.DTOs;
using DocMap.Application.Requests;
using DocMap.Application.Responses;

namespace DocMap.Application.Interfaces;

public interface IProjectService
{
    Task<ApiResponse<IEnumerable<ProjectDto>>> GetAllAsync(int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectDto>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectDto>> CreateAsync(CreateProjectRequest request, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectDto>> UpdateAsync(int id, UpdateProjectRequest request, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
}
