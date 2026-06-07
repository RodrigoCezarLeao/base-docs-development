using Dapper;
using TemperatureApi.Domain.Models;
using TemperatureApi.Infrastructure.Data;
using TemperatureApi.Infrastructure.Repositories.Base;
using TemperatureApi.Infrastructure.Repositories.Interfaces;

namespace TemperatureApi.Infrastructure.Repositories;

public class TemperatureReadingRepository(IDbConnectionFactory connectionFactory)
    : BaseRepository<TemperatureReading, int>(connectionFactory), ITemperatureReadingRepository
{
    public override async Task<TemperatureReading?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM TemperatureReadings WHERE Id = @Id AND IsActive = 1";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<TemperatureReading>(sql, new { Id = id }), cancellationToken);
    }

    public override async Task<IEnumerable<TemperatureReading>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM TemperatureReadings WHERE IsActive = 1 ORDER BY RecordedAt DESC";
        return await QueryAsync(conn => conn.QueryAsync<TemperatureReading>(sql), cancellationToken);
    }

    public async Task<(IEnumerable<TemperatureReading> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT * FROM TemperatureReadings WHERE IsActive = 1
            ORDER BY RecordedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM TemperatureReadings WHERE IsActive = 1;";

        return await QueryAsync(async conn =>
        {
            using var multi = await conn.QueryMultipleAsync(sql, new
            {
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            });
            var items = await multi.ReadAsync<TemperatureReading>();
            var totalCount = await multi.ReadFirstAsync<int>();
            return (items, totalCount);
        }, cancellationToken);
    }

    public override async Task<int> CreateAsync(TemperatureReading entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO TemperatureReadings (Location, ValueCelsius, RecordedAt, IsActive, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Location, @ValueCelsius, @RecordedAt, @IsActive, @CreatedAt)";

        entity.CreatedAt = DateTime.UtcNow;
        return await QueryAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, entity), cancellationToken);
    }

    public override async Task<bool> UpdateAsync(TemperatureReading entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE TemperatureReadings
            SET Location = @Location, ValueCelsius = @ValueCelsius,
                RecordedAt = @RecordedAt, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        entity.UpdatedAt = DateTime.UtcNow;
        var affected = await QueryAsync(conn => conn.ExecuteAsync(sql, entity), cancellationToken);
        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE TemperatureReadings SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id";
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }), cancellationToken);
        return affected > 0;
    }
}
