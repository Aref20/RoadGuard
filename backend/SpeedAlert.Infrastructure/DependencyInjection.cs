using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Infrastructure.Persistence;
using SpeedAlert.Infrastructure.Services;

namespace SpeedAlert.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var conn = config.GetConnectionString("DefaultConnection") 
                   ?? "Host=localhost;Port=5432;Database=speedalert;Username=postgres;Password=password";

        services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(conn));
        
        // Expose IAppDbContext for Application layer abstraction
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        
        services.AddScoped<ISpeedLimitProvider, GoogleRoadsProvider>();

        return services;
    }
}
