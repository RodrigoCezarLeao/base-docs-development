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
        const string sql = "SELECT * FROM temperature_readings WHERE id = @Id AND is_active = true";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<TemperatureReading>(sql, new { Id = id }), cancellationToken);
    }

    public override async Task<IEnumerable<TemperatureReading>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM temperature_readings WHERE is_active = true ORDER BY recorded_at DESC";
        return await QueryAsync(conn => conn.QueryAsync<TemperatureReading>(sql), cancellationToken);
    }

    public async Task<(IEnumerable<TemperatureReading> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT * FROM temperature_readings WHERE is_active = true
            ORDER BY recorded_at DESC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM temperature_readings WHERE is_active = true;";

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
            INSERT INTO temperature_readings (location, value_celsius, recorded_at, is_active, created_at)
            VALUES (@Location, @ValueCelsius, @RecordedAt, @IsActive, @CreatedAt)
            RETURNING id";

        entity.CreatedAt = DateTime.UtcNow;
        return await QueryAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, entity), cancellationToken);
    }

    public override async Task<bool> UpdateAsync(TemperatureReading entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE temperature_readings
            SET location      = @Location,
                value_celsius = @ValueCelsius,
                recorded_at   = @RecordedAt,
                is_active     = @IsActive,
                updated_at    = @UpdatedAt
            WHERE id = @Id";

        entity.UpdatedAt = DateTime.UtcNow;
        var affected = await QueryAsync(conn => conn.ExecuteAsync(sql, entity), cancellationToken);
        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE temperature_readings SET is_active = false, updated_at = @UpdatedAt WHERE id = @Id";
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }), cancellationToken);
        return affected > 0;
    }
}
