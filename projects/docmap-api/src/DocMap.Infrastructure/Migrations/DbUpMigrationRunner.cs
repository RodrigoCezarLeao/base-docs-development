using System.Reflection;
using DbUp;
using DbUp.Engine;

namespace DocMap.Infrastructure.Migrations;

public class DbUpMigrationRunner(string connectionString) : IMigrationRunner
{
    public void Run()
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        DatabaseUpgradeResult result = upgrader.PerformUpgrade();

        if (!result.Successful)
            throw new Exception("Database migration failed.", result.Error);
    }
}
