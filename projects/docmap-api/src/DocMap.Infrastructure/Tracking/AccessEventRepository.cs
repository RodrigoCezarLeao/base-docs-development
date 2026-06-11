using Dapper;
using DocMap.Application.Responses;
using DocMap.Application.Tracking;
using DocMap.Domain.Models;
using DocMap.Infrastructure.Data;

namespace DocMap.Infrastructure.Tracking;

public sealed class AccessEventRepository(IDbConnectionFactory connectionFactory) : IAccessEventRepository
{
    public async Task InsertManyAsync(IReadOnlyList<AccessEvent> events, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO access_events
                (user_id, ip, user_agent, browser, os, device_type, method, path, status_code, country, city, occurred_at)
            VALUES
                (@UserId, @Ip, @UserAgent, @Browser, @Os, @DeviceType, @Method, @Path, @StatusCode, @Country, @City, @OccurredAt)";

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, events);
    }

    public async Task<PagedResponse<AccessEventDto>> QueryAsync(AccessQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var filters = new List<string>();
        if (query.UserId is not null) filters.Add("user_id = @UserId");
        if (query.From is not null) filters.Add("occurred_at >= @From");
        if (query.To is not null) filters.Add("occurred_at <= @To");
        if (!string.IsNullOrWhiteSpace(query.Q)) filters.Add("(path ILIKE @Like OR ip ILIKE @Like OR browser ILIKE @Like)");
        var where = filters.Count > 0 ? "WHERE " + string.Join(" AND ", filters) : string.Empty;

        var sql = $@"
            SELECT id, user_id, ip, browser, os, device_type, method, path, status_code, country, city, occurred_at
            FROM access_events {where}
            ORDER BY occurred_at DESC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM access_events {where};";

        var parameters = new
        {
            query.UserId,
            query.From,
            query.To,
            Like = $"%{query.Q}%",
            PageSize = pageSize,
            Offset = (page - 1) * pageSize,
        };

        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var rows = await multi.ReadAsync<AccessEvent>();
        var total = await multi.ReadFirstAsync<int>();

        var items = rows.Select(e => new AccessEventDto(
            e.Id, e.UserId, e.Ip, e.Browser, e.Os, e.DeviceType, e.Method, e.Path, e.StatusCode, e.Country, e.City, e.OccurredAt)).ToList();

        return new PagedResponse<AccessEventDto>(items, total, page, pageSize);
    }

    public async Task<int> DeleteByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM access_events WHERE user_id = @UserId";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(sql, new { UserId = userId });
    }
}
