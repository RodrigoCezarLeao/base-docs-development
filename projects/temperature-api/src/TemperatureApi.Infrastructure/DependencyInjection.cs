using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemperatureApi.Infrastructure.Data;
using TemperatureApi.Infrastructure.Repositories;
using TemperatureApi.Infrastructure.Repositories.Interfaces;

namespace TemperatureApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));
        services.AddScoped<ITemperatureReadingRepository, TemperatureReadingRepository>();

        return services;
    }
}
