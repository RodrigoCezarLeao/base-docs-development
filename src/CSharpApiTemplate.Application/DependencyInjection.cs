using CSharpApiTemplate.Application.Interfaces;
using CSharpApiTemplate.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpApiTemplate.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
