using System.Data;
using Microsoft.Data.SqlClient;

namespace CSharpApiTemplate.Infrastructure.Data;

public class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
