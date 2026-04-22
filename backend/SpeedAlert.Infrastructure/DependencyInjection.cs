using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Infrastructure.Persistence;
using SpeedAlert.Infrastructure.Services;
using SpeedAlert.Infrastructure.Options;
using System;
using System.Net.Http.Headers;

namespace SpeedAlert.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var conn = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conn)) 
        {
            throw new InvalidOperationException("CRITICAL STARTUP ERROR: Missing required configuration 'ConnectionStrings:DefaultConnection'!");
        }

        services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(conn));
        
        // Expose IAppDbContext for Application layer abstraction
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.Configure<SpeedProviderOptions>(config.GetSection("SpeedProviders"));
        
        services.AddHttpClient<ISpeedLimitProvider, GoogleRoadsProvider>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        services.AddHttpClient<ISpeedLimitProvider, HereSpeedLimitProvider>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        services.AddHttpClient<ISpeedLimitProvider, TomTomSpeedLimitProvider>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        return services;
    }
}
