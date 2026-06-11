using Dapper;
using TemperatureApi.Application.Tracking;
using TemperatureApi.Domain.Models;
using TemperatureApi.Infrastructure.Data;

namespace TemperatureApi.Infrastructure.Tracking;

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
