using Dapper;
using DocMap.Domain.Models;
using DocMap.Infrastructure.Data;
using DocMap.Infrastructure.Repositories.Base;
using DocMap.Application.Interfaces;

namespace DocMap.Infrastructure.Repositories;

public class DocumentRepository(IDbConnectionFactory connectionFactory)
    : BaseRepository<Document, int>(connectionFactory), IDocumentRepository
{
    public override async Task<Document?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM documents WHERE id = @Id AND is_active = TRUE";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<Document>(sql, new { Id = id }), cancellationToken);
    }

    public override async Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM documents WHERE is_active = TRUE ORDER BY created_at DESC";
        return await QueryAsync(conn =>
            conn.QueryAsync<Document>(sql), cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetAllByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM documents WHERE project_id = @ProjectId AND is_active = TRUE ORDER BY created_at DESC";
        return await QueryAsync(conn =>
            conn.QueryAsync<Document>(sql, new { ProjectId = projectId }), cancellationToken);
    }

    public override async Task<int> CreateAsync(Document entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO documents (project_id, title, file_path, content, canvas_x, canvas_y, is_active, created_at)
            VALUES (@ProjectId, @Title, @FilePath, @Content, @CanvasX, @CanvasY, @IsActive, @CreatedAt)
            RETURNING id";

        return await QueryAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, entity), cancellationToken);
    }

    public override async Task<bool> UpdateAsync(Document entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE documents
            SET title = @Title, file_path = @FilePath, content = @Content, updated_at = @UpdatedAt
            WHERE id = @Id";

        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, entity), cancellationToken);
        return affected > 0;
    }

    public async Task<bool> UpdatePositionAsync(int id, double canvasX, double canvasY, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE documents
            SET canvas_x = @CanvasX, canvas_y = @CanvasY, updated_at = @UpdatedAt
            WHERE id = @Id";

        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, new { Id = id, CanvasX = canvasX, CanvasY = canvasY, UpdatedAt = DateTime.UtcNow }), cancellationToken);
        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE documents SET is_active = FALSE, updated_at = @UpdatedAt WHERE id = @Id";
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }), cancellationToken);
        return affected > 0;
    }
}
