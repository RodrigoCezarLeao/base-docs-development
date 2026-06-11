using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemperatureApi.Application.Caching;
using TemperatureApi.Application.Interfaces;
using TemperatureApi.Infrastructure.Caching;
using TemperatureApi.Infrastructure.Data;
using TemperatureApi.Infrastructure.Migrations;
using TemperatureApi.Infrastructure.Repositories;

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

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddScoped<ITemperatureReadingRepository, TemperatureReadingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
