using System.Data;
using Microsoft.Data.SqlClient;

namespace TemperatureApi.Infrastructure.Data;

public class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
