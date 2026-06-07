using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemperatureApi.Infrastructure.Data;
using TemperatureApi.Infrastructure.Migrations;
using TemperatureApi.Infrastructure.Repositories;
using TemperatureApi.Application.Interfaces;

namespace TemperatureApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // snake_case (postgres) → PascalCase (C#) automático
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));
        services.AddSingleton<IMigrationRunner>(_ => new DbUpMigrationRunner(connectionString));
        services.AddScoped<ITemperatureReadingRepository, TemperatureReadingRepository>();

        return services;
    }
}
