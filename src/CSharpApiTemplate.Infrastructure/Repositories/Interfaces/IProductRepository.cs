using CSharpApiTemplate.Domain.Models;
using CSharpApiTemplate.Infrastructure.Repositories.Base;

namespace CSharpApiTemplate.Infrastructure.Repositories.Interfaces;

public interface IProductRepository : IBaseRepository<Product, int>
{
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
