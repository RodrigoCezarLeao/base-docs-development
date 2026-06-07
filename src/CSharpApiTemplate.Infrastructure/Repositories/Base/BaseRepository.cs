using System.Data;
using CSharpApiTemplate.Infrastructure.Data;

namespace CSharpApiTemplate.Infrastructure.Repositories.Base;

public abstract class BaseRepository<T, TKey>(IDbConnectionFactory connectionFactory)
    : IBaseRepository<T, TKey> where T : class
{
    protected readonly IDbConnectionFactory ConnectionFactory = connectionFactory;

    protected IDbConnection CreateConnection() => ConnectionFactory.CreateConnection();

    protected async Task<TResult> QueryAsync<TResult>(
        Func<IDbConnection, Task<TResult>> query,
        CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        return await query(connection);
    }

    protected async Task<TResult> QueryInTransactionAsync<TResult>(
        Func<IDbConnection, IDbTransaction, Task<TResult>> query,
        CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var result = await query(connection, transaction);
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public abstract Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    public abstract Task<TKey> CreateAsync(T entity, CancellationToken cancellationToken = default);
    public abstract Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    public abstract Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
