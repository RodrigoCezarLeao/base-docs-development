using DocMap.Application.Interfaces;
using DocMap.Application.Logging;
using DocMap.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocMap.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddScoped<ILogReader, LogReaderService>();
        return services;
    }
}
