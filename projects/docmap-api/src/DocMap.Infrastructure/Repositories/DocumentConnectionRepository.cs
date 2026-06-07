using Dapper;
using DocMap.Domain.Models;
using DocMap.Infrastructure.Data;
using DocMap.Infrastructure.Repositories.Base;
using DocMap.Infrastructure.Repositories.Interfaces;

namespace DocMap.Infrastructure.Repositories;

public class DocumentConnectionRepository(IDbConnectionFactory connectionFactory)
    : BaseRepository<DocumentConnection, int>(connectionFactory), IDocumentConnectionRepository
{
    public override async Task<DocumentConnection?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM document_connections WHERE id = @Id AND is_active = TRUE";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<DocumentConnection>(sql, new { Id = id }), cancellationToken);
    }

    public override async Task<IEnumerable<DocumentConnection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM document_connections WHERE is_active = TRUE ORDER BY created_at DESC";
        return await QueryAsync(conn =>
            conn.QueryAsync<DocumentConnection>(sql), cancellationToken);
    }

    public async Task<IEnumerable<DocumentConnection>> GetAllByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM document_connections WHERE project_id = @ProjectId AND is_active = TRUE ORDER BY created_at DESC";
        return await QueryAsync(conn =>
            conn.QueryAsync<DocumentConnection>(sql, new { ProjectId = projectId }), cancellationToken);
    }

    public async Task<DocumentConnection?> GetBySourceAndTargetAsync(int sourceDocumentId, int targetDocumentId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT * FROM document_connections
            WHERE source_document_id = @SourceDocumentId
              AND target_document_id = @TargetDocumentId
              AND is_active = TRUE";

        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<DocumentConnection>(sql, new
            {
                SourceDocumentId = sourceDocumentId,
                TargetDocumentId = targetDocumentId
            }), cancellationToken);
    }

    public override async Task<int> CreateAsync(DocumentConnection entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO document_connections (project_id, source_document_id, target_document_id, label, is_active, created_at)
            VALUES (@ProjectId, @SourceDocumentId, @TargetDocumentId, @Label, @IsActive, @CreatedAt)
            RETURNING id";

        return await QueryAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, entity), cancellationToken);
    }

    public override async Task<bool> UpdateAsync(DocumentConnection entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE document_connections
            SET label = @Label
            WHERE id = @Id";

        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, entity), cancellationToken);
        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE document_connections SET is_active = FALSE WHERE id = @Id";
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, new { Id = id }), cancellationToken);
        return affected > 0;
    }
}
