using CSharpApiTemplate.Application.DTOs;
using CSharpApiTemplate.Application.Requests;
using CSharpApiTemplate.Application.Responses;

namespace CSharpApiTemplate.Application.Interfaces;

public interface IProductService
{
    Task<ApiResponse<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<ProductDto>>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
