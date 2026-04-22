using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Infrastructure.Persistence;
using SpeedAlert.Infrastructure.Services;
using System;

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
        
        services.AddHttpClient<ISpeedLimitProvider, GoogleRoadsProvider>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        services.AddHttpClient<ISpeedLimitProvider, HereSpeedLimitProvider>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        services.AddHttpClient<ISpeedLimitProvider, TomTomSpeedLimitProvider>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        return services;
    }
}
