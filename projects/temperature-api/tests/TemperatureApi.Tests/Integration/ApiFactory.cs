using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using TemperatureApi.Infrastructure.Data;
using TemperatureApi.Infrastructure.Migrations;
using TemperatureApi.Application.Interfaces;

namespace TemperatureApi.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>
{
    public ITemperatureReadingRepository RepositoryMock { get; } =
        Substitute.For<ITemperatureReadingRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Fornece connection string falsa para que AddInfrastructure não jogue exceção
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Substitui serviços de infraestrutura por mocks (sem banco real)
            services.RemoveAll<IDbConnectionFactory>();
            services.RemoveAll<ITemperatureReadingRepository>();
            services.RemoveAll<IMigrationRunner>();

            services.AddSingleton(RepositoryMock);
            services.AddSingleton<IMigrationRunner, NoOpMigrationRunner>();
        });
    }
}

internal sealed class NoOpMigrationRunner : IMigrationRunner
{
    public void Run() { }
}
