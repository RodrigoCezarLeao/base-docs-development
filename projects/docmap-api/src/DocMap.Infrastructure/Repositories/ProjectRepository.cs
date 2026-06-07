using Dapper;
using DocMap.Domain.Models;
using DocMap.Infrastructure.Data;
using DocMap.Infrastructure.Repositories.Base;
using DocMap.Application.Interfaces;

namespace DocMap.Infrastructure.Repositories;

public class ProjectRepository(IDbConnectionFactory connectionFactory)
    : BaseRepository<Project, int>(connectionFactory), IProjectRepository
{
    public override async Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM projects WHERE id = @Id AND is_active = TRUE";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<Project>(sql, new { Id = id }), cancellationToken);
    }

    public override async Task<IEnumerable<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM projects WHERE is_active = TRUE ORDER BY created_at DESC";
        return await QueryAsync(conn =>
            conn.QueryAsync<Project>(sql), cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetAllByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM projects WHERE user_id = @UserId AND is_active = TRUE ORDER BY created_at DESC";
        return await QueryAsync(conn =>
            conn.QueryAsync<Project>(sql, new { UserId = userId }), cancellationToken);
    }

    public async Task<int> GetDocumentCountAsync(int projectId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM documents WHERE project_id = @ProjectId AND is_active = TRUE";
        return await QueryAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, new { ProjectId = projectId }), cancellationToken);
    }

    public override async Task<int> CreateAsync(Project entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO projects (user_id, name, description, is_active, created_at)
            VALUES (@UserId, @Name, @Description, @IsActive, @CreatedAt)
            RETURNING id";

        return await QueryAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, entity), cancellationToken);
    }

    public override async Task<bool> UpdateAsync(Project entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE projects
            SET name = @Name, description = @Description, updated_at = @UpdatedAt
            WHERE id = @Id";

        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, entity), cancellationToken);
        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE projects SET is_active = FALSE, updated_at = @UpdatedAt WHERE id = @Id";
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }), cancellationToken);
        return affected > 0;
    }
}
