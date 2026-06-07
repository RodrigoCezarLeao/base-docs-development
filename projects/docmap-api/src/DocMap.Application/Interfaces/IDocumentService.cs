using DocMap.Application.DTOs;
using DocMap.Application.Requests;
using DocMap.Application.Responses;
using DocMap.Domain.Models;

namespace DocMap.Application.Interfaces;

public interface IDocumentService
{
    Task<ApiResponse<IEnumerable<DocumentDto>>> GetAllAsync(int projectId, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<DocumentDto>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<DocumentDto>> CreateAsync(int projectId, CreateDocumentRequest request, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<DocumentDto>> UpdateAsync(int id, UpdateDocumentRequest request, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<DocumentDto>> UpdatePositionAsync(int id, UpdateDocumentPositionRequest request, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<byte[]>> ExportProjectAsync(int projectId, int userId, CancellationToken cancellationToken = default);
}
