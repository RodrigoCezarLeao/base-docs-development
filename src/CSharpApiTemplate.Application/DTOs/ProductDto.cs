namespace CSharpApiTemplate.Application.DTOs;

public record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
