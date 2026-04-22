using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpeedAlert.Application.Models;
using SpeedAlert.Domain.Entities;
using SpeedAlert.Infrastructure.Persistence;

namespace SpeedAlert.Api.Services;

public static class ApplicationInitializationService
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        bool runMigrations,
        string adminEmail,
        string adminPassword,
        CancellationToken cancellationToken = default)
    {
        adminEmail = adminEmail.Trim().ToLowerInvariant();

        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("ApplicationInitialization");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (runMigrations)
        {
            logger.LogInformation("Applying database migrations on startup.");
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (!await db.Users.AnyAsync(user => user.Email == adminEmail, cancellationToken))
        {
            logger.LogInformation("Seeding default admin user {Email}.", adminEmail);
            var adminUser = new User
            {
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = "Admin",
                IsActive = true
            };

            adminUser.Settings = new UserSettings { UserId = adminUser.Id };
            db.Users.Add(adminUser);
        }

        await EnsureProviderConfigsExistAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureProviderConfigsExistAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var configs = await db.ProviderConfigs.ToListAsync(cancellationToken);
        foreach (var providerKey in SpeedProviderKeys.All)
        {
            if (configs.Any(config => config.ProviderKey == providerKey))
            {
                continue;
            }

            var providerConfig = new ProviderConfig
            {
                ProviderKey = providerKey,
                IsEnabled = true,
                IsSelected = providerKey == SpeedProviderKeys.Google,
                PriorityOrder = configs.Count,
                LastHealthStatus = "Unknown",
                UpdatedAt = DateTime.UtcNow
            };

            db.ProviderConfigs.Add(providerConfig);
            configs.Add(providerConfig);
        }

        if (configs.All(config => !config.IsSelected))
        {
            var googleConfig = db.ProviderConfigs.Local.FirstOrDefault(config => config.ProviderKey == SpeedProviderKeys.Google) ??
                               configs.FirstOrDefault(config => config.ProviderKey == SpeedProviderKeys.Google);

            if (googleConfig != null)
            {
                googleConfig.IsSelected = true;
            }
        }
    }
}
