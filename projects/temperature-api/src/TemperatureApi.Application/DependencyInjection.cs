using Microsoft.Extensions.DependencyInjection;
using TemperatureApi.Application.Interfaces;
using TemperatureApi.Application.Services;

namespace TemperatureApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITemperatureReadingService, TemperatureReadingService>();
        return services;
    }
}
