using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using System.Threading.Tasks;
using System.Linq;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAppDbContext _db;

    public AdminController(IAppDbContext db) => _db = db;

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .Select(u => new { u.Id, u.Email, u.IsActive, u.CreatedAt })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var sessions = await _db.Sessions
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
            .ToListAsync();
        return Ok(sessions);
    }

    [HttpGet("health-overview")]
    public async Task<IActionResult> GetSystemHealth()
    {
        var totalUsers = await _db.Users.CountAsync();
        var totalSessions = await _db.Sessions.CountAsync();
        var activeSessions = await _db.Sessions.CountAsync(s => s.Status == "Active");
        var autoStartedSessions = await _db.Sessions.CountAsync(s => s.WasAutoStarted);
        
        var totalViolations = await _db.Sessions.SumAsync(s => s.OverspeedEventCount);
        var totalAlerts = await _db.Sessions.SumAsync(s => s.AlertEventCount);

        bool dbHealthy = false;
        try {
            dbHealthy = await ((Microsoft.EntityFrameworkCore.DbContext)_db).Database.CanConnectAsync();
        } catch { }

        return Ok(new
        {
            TotalUsers = totalUsers,
            TotalSessions = totalSessions,
            ActiveSessions = activeSessions,
            AutoStartedSessions = autoStartedSessions,
            TotalViolations = totalViolations,
            TotalAlerts = totalAlerts,
            ServerTime = System.DateTime.UtcNow,
            ApiVersion = "1.0",
            DatabaseStatus = dbHealthy ? "Healthy" : "Disconnected"
        });
    }

    [HttpGet("provider-settings")]
    public async Task<IActionResult> GetProviderSettings()
    {
        var configs = await _db.ProviderConfigs.OrderBy(p => p.PriorityOrder).ToListAsync();
        
        // Seed default providers if missed by migration
        var defaultProviders = new[] { "GoogleRoads", "Here", "TomTom" };
        bool addedNew = false;
        foreach (var pKey in defaultProviders)
        {
            if (!configs.Any(c => c.ProviderKey == pKey))
            {
                var newConfig = new SpeedAlert.Domain.Entities.ProviderConfig
                {
                    ProviderKey = pKey,
                    IsEnabled = true,
                    IsSelected = pKey == "GoogleRoads",
                    PriorityOrder = pKey == "GoogleRoads" ? 0 : 1,
                    UpdatedAt = System.DateTime.UtcNow
                };
                _db.ProviderConfigs.Add(newConfig);
                configs.Add(newConfig);
                addedNew = true;
            }
        }
        
        if (addedNew)
        {
            await _db.SaveChangesAsync();
        }

        return Ok(configs.Select(c => new
        {
            c.ProviderKey,
            DisplayName = c.ProviderKey,
            c.IsEnabled,
            c.IsSelected,
            c.PriorityOrder,
            c.UpdatedAt
        }));
    }

    [HttpPut("provider-settings")]
    public async Task<IActionResult> UpdateProviderSettings([FromBody] System.Collections.Generic.List<ProviderConfigUpdateDto> payload)
    {
        var configs = await _db.ProviderConfigs.ToListAsync();
        
        // Ensure only one is selected
        var selectedCount = payload.Count(p => p.IsSelected);
        if (selectedCount > 1) return BadRequest("Only one provider can be selected");

        foreach (var update in payload)
        {
            var conf = configs.FirstOrDefault(c => c.ProviderKey == update.ProviderKey);
            if (conf != null)
            {
                conf.IsEnabled = update.IsEnabled;
                conf.IsSelected = update.IsSelected;
                conf.PriorityOrder = update.PriorityOrder;
                conf.UpdatedAt = System.DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Provider settings updated successfully" });
    }
}

public class ProviderConfigUpdateDto
{
    public string ProviderKey { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public bool IsSelected { get; set; }
    public int PriorityOrder { get; set; }
}
