using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemperatureApi.Application.Caching;
using TemperatureApi.Application.Interfaces;
using TemperatureApi.Application.Metrics;
using TemperatureApi.Infrastructure.Caching;
using TemperatureApi.Infrastructure.Data;
using TemperatureApi.Application.Tracking;
using TemperatureApi.Infrastructure.Metrics;
using TemperatureApi.Infrastructure.Migrations;
using TemperatureApi.Infrastructure.Repositories;
using TemperatureApi.Infrastructure.Tracking;

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
        services.AddSingleton<IMetricsCollector, MetricsCollector>();
        services.AddScoped<ITemperatureReadingRepository, TemperatureReadingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Access tracking (LGPD): write path is a background queue → repository.
        var anonymizeIp = bool.TryParse(configuration["Tracking:AnonymizeIp"], out var anon) && anon;
        services.AddSingleton(new IpAnonymizer(anonymizeIp));
        services.AddSingleton<AccessEventQueue>();
        services.AddSingleton<IAccessTracker>(sp => sp.GetRequiredService<AccessEventQueue>());
        services.AddHostedService<AccessEventWriter>();
        services.AddScoped<IAccessEventRepository, AccessEventRepository>();
        services.AddScoped<IConsentRepository, ConsentRepository>();

        return services;
    }
}
