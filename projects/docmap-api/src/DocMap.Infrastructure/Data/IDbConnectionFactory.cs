using System.Data;

namespace DocMap.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
