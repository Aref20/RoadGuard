using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Domain.Entities;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IAppDbContext _db;

    public UsersController(IAppDbContext db) => _db = db;

    private System.Guid GetUserId() => System.Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == GetUserId());
            
        if (user == null) return NotFound();
        return Ok(new { user.Id, user.Email, user.Role, user.CreatedAt });
    }

    [HttpGet("me/settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _db.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == GetUserId());
            
        if (settings == null) return NotFound();
        return Ok(settings);
    }

    [HttpPut("me/settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UserSettings updatedSettings)
    {
        var settings = await _db.UserSettings.FirstOrDefaultAsync(s => s.UserId == GetUserId());
        if (settings == null) return NotFound();

        settings.SpeedUnit = updatedSettings.SpeedUnit;
        settings.OverspeedTolerance = updatedSettings.OverspeedTolerance;
        settings.AlertDelaySeconds = updatedSettings.AlertDelaySeconds;
        settings.AlertCooldownSeconds = updatedSettings.AlertCooldownSeconds;
        settings.SoundEnabled = updatedSettings.SoundEnabled;
        settings.VibrationEnabled = updatedSettings.VibrationEnabled;
        settings.AutoDetectDrivingEnabled = updatedSettings.AutoDetectDrivingEnabled;
        settings.AutoStartMonitoringEnabled = updatedSettings.AutoStartMonitoringEnabled;

        await _db.SaveChangesAsync();
        return Ok(settings);
    }
}
