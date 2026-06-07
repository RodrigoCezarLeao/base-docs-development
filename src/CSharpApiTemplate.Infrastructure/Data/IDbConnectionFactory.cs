using System.Data;

namespace CSharpApiTemplate.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
