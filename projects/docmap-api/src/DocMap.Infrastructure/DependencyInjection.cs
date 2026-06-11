using Dapper;
using DocMap.Application.Caching;
using DocMap.Application.Interfaces;
using DocMap.Application.Metrics;
using DocMap.Infrastructure.Caching;
using DocMap.Infrastructure.Data;
using DocMap.Application.Tracking;
using DocMap.Infrastructure.Metrics;
using DocMap.Infrastructure.Migrations;
using DocMap.Infrastructure.Repositories;
using DocMap.Infrastructure.Tracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocMap.Infrastructure;

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

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentConnectionRepository, DocumentConnectionRepository>();

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
