using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemperatureApi.Application.Interfaces;
using TemperatureApi.Application.Logging;
using TemperatureApi.Application.Services;
using TemperatureApi.Application.Tracking;

namespace TemperatureApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITemperatureReadingService, TemperatureReadingService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILogReader, LogReaderService>();
        services.AddScoped<ITrackingService, TrackingService>();
        return services;
    }
}
