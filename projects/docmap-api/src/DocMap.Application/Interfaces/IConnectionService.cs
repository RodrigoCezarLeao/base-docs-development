using DocMap.Application.DTOs;
using DocMap.Application.Requests;
using DocMap.Application.Responses;

namespace DocMap.Application.Interfaces;

public interface IConnectionService
{
    Task<ApiResponse<IEnumerable<DocumentConnectionDto>>> GetAllAsync(int projectId, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<DocumentConnectionDto>> CreateAsync(int projectId, CreateConnectionRequest request, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, int projectId, int userId, CancellationToken cancellationToken = default);
}
