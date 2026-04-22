using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpeedAlert.Api.Hubs;
using SpeedAlert.Application.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedAlert.Api.Services;

public class TelemetryBroadcastService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<TelemetryHub> _hubContext;

    public TelemetryBroadcastService(IServiceProvider serviceProvider, IHubContext<TelemetryHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                
                // Fetch Health
                var totalUsers = await db.Users.CountAsync(stoppingToken);
                var totalSessions = await db.Sessions.CountAsync(stoppingToken);
                var activeSessions = await db.Sessions.CountAsync(s => s.Status == "Active", stoppingToken);
                var autoStartedSessions = await db.Sessions.CountAsync(s => s.WasAutoStarted, stoppingToken);
                var totalViolations = await db.Sessions.SumAsync(s => s.OverspeedEventCount, stoppingToken);
                var totalAlerts = await db.Sessions.SumAsync(s => s.AlertEventCount, stoppingToken);

                bool dbHealthy = false;
                try {
                    dbHealthy = await ((DbContext)db).Database.CanConnectAsync(stoppingToken);
                } catch { }

                var health = new
                {
                    TotalUsers = totalUsers,
                    TotalSessions = totalSessions,
                    ActiveSessions = activeSessions,
                    AutoStartedSessions = autoStartedSessions,
                    TotalViolations = totalViolations,
                    TotalAlerts = totalAlerts,
                    ServerTime = DateTime.UtcNow,
                    DatabaseStatus = dbHealthy ? "Healthy" : "Disconnected"
                };

                // Fetch Users
                var users = await db.Users
                    .Select(u => new { u.Id, u.Email, u.IsActive, u.CreatedAt })
                    .ToListAsync(stoppingToken);

                // Fetch Sessions
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

                // Broadcast
                await _hubContext.Clients.All.SendAsync("ReceiveHealth", health, stoppingToken);
                await _hubContext.Clients.All.SendAsync("ReceiveUsers", users, stoppingToken);
                await _hubContext.Clients.All.SendAsync("ReceiveSessions", sessions, stoppingToken);
            }
            catch (Exception)
            {
                // Optionally log exception, but keep loop alive
            }

            await Task.Delay(10000, stoppingToken); // Broadcast every 10 seconds
        }
    }
}
