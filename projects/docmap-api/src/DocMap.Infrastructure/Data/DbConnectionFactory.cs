using System.Data;
using Npgsql;

namespace DocMap.Infrastructure.Data;

public class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new NpgsqlConnection(connectionString);
}
