using DocMap.Infrastructure.Data;
using DocMap.Infrastructure.Migrations;
using DocMap.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace DocMap.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>
{
    public IUserRepository UserRepositoryMock { get; } = Substitute.For<IUserRepository>();
    public IProjectRepository ProjectRepositoryMock { get; } = Substitute.For<IProjectRepository>();
    public IDocumentRepository DocumentRepositoryMock { get; } = Substitute.For<IDocumentRepository>();
    public IDocumentConnectionRepository ConnectionRepositoryMock { get; } = Substitute.For<IDocumentConnectionRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Fornece connection string falsa e Jwt:Secret para que os serviços não joguem exceção
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Jwt:Secret"] = "test-jwt-secret-key-for-testing-min32chars!!"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Substitui serviços de infraestrutura por mocks (sem banco real)
            services.RemoveAll<IDbConnectionFactory>();
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IProjectRepository>();
            services.RemoveAll<IDocumentRepository>();
            services.RemoveAll<IDocumentConnectionRepository>();
            services.RemoveAll<IMigrationRunner>();

            services.AddSingleton(UserRepositoryMock);
            services.AddSingleton(ProjectRepositoryMock);
            services.AddSingleton(DocumentRepositoryMock);
            services.AddSingleton(ConnectionRepositoryMock);
            services.AddSingleton<IMigrationRunner, NoOpMigrationRunner>();
        });
    }
}

internal sealed class NoOpMigrationRunner : IMigrationRunner
{
    public void Run() { }
}
