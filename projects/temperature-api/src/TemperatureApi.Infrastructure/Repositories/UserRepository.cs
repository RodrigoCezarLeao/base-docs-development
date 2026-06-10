using Dapper;
using TemperatureApi.Application.Interfaces;
using TemperatureApi.Domain.Models;
using TemperatureApi.Infrastructure.Data;
using TemperatureApi.Infrastructure.Repositories.Base;

namespace TemperatureApi.Infrastructure.Repositories;

public class UserRepository(IDbConnectionFactory connectionFactory)
    : BaseRepository<User, int>(connectionFactory), IUserRepository
{
    public override async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM users WHERE id = @Id AND is_active = TRUE";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<User>(sql, new { Id = id }), cancellationToken);
    }

    public override async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM users WHERE is_active = TRUE ORDER BY created_at DESC";
        return await QueryAsync(conn =>
            conn.QueryAsync<User>(sql), cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM users WHERE email = @Email AND is_active = TRUE";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<User>(sql, new { Email = email }), cancellationToken);
    }

    public override async Task<int> CreateAsync(User entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO users (email, password_hash, name, is_admin, is_active, created_at)
            VALUES (@Email, @PasswordHash, @Name, @IsAdmin, @IsActive, @CreatedAt)
            RETURNING id";

        return await QueryAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, entity), cancellationToken);
    }

    public override async Task<bool> UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE users
            SET email = @Email, password_hash = @PasswordHash, name = @Name,
                is_admin = @IsAdmin, is_active = @IsActive, updated_at = @UpdatedAt
            WHERE id = @Id";

        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, entity), cancellationToken);
        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE users SET is_active = FALSE, updated_at = @UpdatedAt WHERE id = @Id";
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }), cancellationToken);
        return affected > 0;
    }
}
