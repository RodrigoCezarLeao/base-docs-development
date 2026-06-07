using CSharpApiTemplate.Application.DTOs;
using CSharpApiTemplate.Application.Interfaces;
using CSharpApiTemplate.Application.Requests;
using CSharpApiTemplate.Application.Responses;
using CSharpApiTemplate.Domain.Models;
using CSharpApiTemplate.Infrastructure.Repositories.Interfaces;

namespace CSharpApiTemplate.Application.Services;

public class ProductService(IProductRepository productRepository) : IProductService
{
    public async Task<ApiResponse<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return ApiResponse<ProductDto>.Fail($"Product with id {id} not found.");

        return ApiResponse<ProductDto>.Ok(MapToDto(product));
    }

    public async Task<ApiResponse<PagedResponse<ProductDto>>> GetAllAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await productRepository.GetPagedAsync(page, pageSize, cancellationToken);
        var paged = new PagedResponse<ProductDto>(items.Select(MapToDto), totalCount, page, pageSize);
        return ApiResponse<PagedResponse<ProductDto>>.Ok(paged);
    }

    public async Task<ApiResponse<ProductDto>> CreateAsync(
        CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            IsActive = true
        };

        var id = await productRepository.CreateAsync(product, cancellationToken);
        product.Id = id;
        return ApiResponse<ProductDto>.Created(MapToDto(product));
    }

    public async Task<ApiResponse<ProductDto>> UpdateAsync(
        int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await productRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return ApiResponse<ProductDto>.Fail($"Product with id {id} not found.");

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Price = request.Price;
        existing.Stock = request.Stock;
        existing.IsActive = request.IsActive;

        await productRepository.UpdateAsync(existing, cancellationToken);
        return ApiResponse<ProductDto>.Ok(MapToDto(existing), "Product updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await productRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return ApiResponse<bool>.Fail($"Product with id {id} not found.");

        await productRepository.DeleteAsync(id, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Product deleted successfully.");
    }

    private static ProductDto MapToDto(Product p) => new(
        p.Id, p.Name, p.Description, p.Price, p.Stock, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
