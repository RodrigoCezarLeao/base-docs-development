using System.ComponentModel.DataAnnotations;

namespace CSharpApiTemplate.Application.Requests;

public record UpdateProductRequest(
    [Required][MaxLength(200)] string Name,
    [MaxLength(1000)] string Description,
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")] decimal Price,
    [Range(0, int.MaxValue, ErrorMessage = "Stock must be non-negative.")] int Stock,
    bool IsActive
);
