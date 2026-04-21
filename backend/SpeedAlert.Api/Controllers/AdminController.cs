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
            DatabaseStatus = "Healthy"
        });
    }
}
