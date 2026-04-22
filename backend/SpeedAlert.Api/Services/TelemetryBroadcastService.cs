using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpeedAlert.Api.Hubs;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedAlert.Api.Services;

public class TelemetryBroadcastService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly ILogger<TelemetryBroadcastService> _logger;

    public TelemetryBroadcastService(
        IServiceProvider serviceProvider,
        IHubContext<TelemetryHub> hubContext,
        ILogger<TelemetryBroadcastService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var orchestrator = scope.ServiceProvider.GetRequiredService<ISpeedLimitProviderOrchestrator>();
                
                var totalUsers = await db.Users.CountAsync(stoppingToken);
                var totalSessions = await db.Sessions.CountAsync(stoppingToken);
                var activeSessions = await db.Sessions.CountAsync(s => s.Status == "Active", stoppingToken);
                var autoStartedSessions = await db.Sessions.CountAsync(s => s.WasAutoStarted, stoppingToken);
                var totalViolations = await db.Sessions.SumAsync(s => s.OverspeedEventCount, stoppingToken);
                var totalAlerts = await db.Sessions.SumAsync(s => s.AlertEventCount, stoppingToken);
                var providerStatuses = await orchestrator.GetProviderStatusesAsync(stoppingToken);
                var selectedProvider = providerStatuses.FirstOrDefault(status => status.IsSelected);

                bool dbHealthy = false;
                try
                {
                    dbHealthy = await ((DbContext)db).Database.CanConnectAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to probe database connectivity for telemetry health.");
                }

                var health = new
                {
                    TotalUsers = totalUsers,
                    TotalSessions = totalSessions,
                    ActiveSessions = activeSessions,
                    AutoStartedSessions = autoStartedSessions,
                    TotalViolations = totalViolations,
                    TotalAlerts = totalAlerts,
                    ServerTime = DateTime.UtcNow,
                    DatabaseStatus = dbHealthy ? "Healthy" : "Disconnected",
                    SelectedProvider = selectedProvider?.ProviderKey,
                    ProviderHealth = selectedProvider?.HealthStatus ?? "Unknown"
                };

                var users = await db.Users
                    .Select(u => new { u.Id, u.Email, u.IsActive, u.CreatedAt })
                    .ToListAsync(stoppingToken);

                var sessions = await db.Sessions
                    .OrderByDescending(s => s.StartedAt)
                    .Take(100)
                    .Select(s => new {
                        s.Id,
                        s.UserId,
                        s.StartedAt,
                        s.EndedAt,
                        s.Status,
                        s.WasAutoStarted,
                        s.SessionStartReason,
                        s.SessionEndReason,
                        s.OverspeedEventCount,
                        s.AlertEventCount
                    })
                    .ToListAsync(stoppingToken);

                await _hubContext.Clients.Group(TelemetryHub.AdminGroup).SendAsync("ReceiveHealth", health, stoppingToken);
                await _hubContext.Clients.Group(TelemetryHub.AdminGroup).SendAsync("ReceiveUsers", users, stoppingToken);
                await _hubContext.Clients.Group(TelemetryHub.AdminGroup).SendAsync("ReceiveSessions", sessions, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telemetry broadcast iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
