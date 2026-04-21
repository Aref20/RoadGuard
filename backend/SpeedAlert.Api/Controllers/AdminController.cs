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
        var activeSessions = await _db.Sessions
            .Where(s => s.Status == "Active")
            .Select(s => new { s.Id, s.UserId, s.StartedAt, s.WasAutoStarted })
            .ToListAsync();
        return Ok(activeSessions);
    }

    [HttpGet("health-overview")]
    public async Task<IActionResult> GetSystemHealth()
    {
        var totalUsers = await _db.Users.CountAsync();
        var totalSessions = await _db.Sessions.CountAsync();

        return Ok(new
        {
            TotalUsers = totalUsers,
            TotalSessions = totalSessions,
            ServerTime = System.DateTime.UtcNow
        });
    }
}
