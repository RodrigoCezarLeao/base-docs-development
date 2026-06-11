using Dapper;
using DocMap.Application.Tracking;
using DocMap.Domain.Models;
using DocMap.Infrastructure.Data;

namespace DocMap.Infrastructure.Tracking;

public sealed class ConsentRepository(IDbConnectionFactory connectionFactory) : IConsentRepository
{
    public async Task InsertAsync(Consent consent, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO consents (user_id, decision, policy_version, ip, occurred_at)
            VALUES (@UserId, @Decision, @PolicyVersion, @Ip, @OccurredAt)";

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, consent);
    }
}
