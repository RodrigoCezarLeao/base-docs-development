using CSharpApiTemplate.Domain.Models;
using CSharpApiTemplate.Infrastructure.Data;
using CSharpApiTemplate.Infrastructure.Repositories.Base;
using CSharpApiTemplate.Infrastructure.Repositories.Interfaces;
using Dapper;

namespace CSharpApiTemplate.Infrastructure.Repositories;

public class ProductRepository(IDbConnectionFactory connectionFactory)
    : BaseRepository<Product, int>(connectionFactory), IProductRepository
{
    public override async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Products WHERE Id = @Id AND IsActive = 1";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<Product>(sql, new { Id = id }), cancellationToken);
    }

    public override async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Products WHERE IsActive = 1 ORDER BY CreatedAt DESC";
        return await QueryAsync(conn => conn.QueryAsync<Product>(sql), cancellationToken);
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT * FROM Products WHERE IsActive = 1
            ORDER BY CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM Products WHERE IsActive = 1;";

        return await QueryAsync(async conn =>
        {
            using var multi = await conn.QueryMultipleAsync(sql, new
            {
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            });
            var items = await multi.ReadAsync<Product>();
            var totalCount = await multi.ReadFirstAsync<int>();
            return (items, totalCount);
        }, cancellationToken);
    }

    public override async Task<int> CreateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Products (Name, Description, Price, Stock, IsActive, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Description, @Price, @Stock, @IsActive, @CreatedAt)";

        entity.CreatedAt = DateTime.UtcNow;
        return await QueryAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, entity), cancellationToken);
    }

    public override async Task<bool> UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Products
            SET Name = @Name, Description = @Description, Price = @Price,
                Stock = @Stock, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        entity.UpdatedAt = DateTime.UtcNow;
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, entity), cancellationToken);
        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Products SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id";
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }), cancellationToken);
        return affected > 0;
    }
}
