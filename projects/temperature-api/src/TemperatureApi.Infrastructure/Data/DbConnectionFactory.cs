using System.Data;
using Npgsql;

namespace TemperatureApi.Infrastructure.Data;

public class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new NpgsqlConnection(connectionString);
}
