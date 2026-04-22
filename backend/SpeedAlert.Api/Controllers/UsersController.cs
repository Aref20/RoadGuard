using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "User")]
public class UsersController : ControllerBase
{
    private readonly IAppDbContext _db;

    public UsersController(IAppDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == GetUserId(), cancellationToken);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(new { user.Id, user.Email, user.Role, user.CreatedAt, user.IsActive });
    }

    [HttpGet("me/settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _db.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == GetUserId(), cancellationToken);

        if (settings == null)
        {
            return NotFound(new { message = "Settings not found." });
        }

        return Ok(new
        {
            settings.SpeedUnit,
            settings.OverspeedTolerance,
            settings.AlertDelaySeconds,
            settings.AlertCooldownSeconds,
            settings.SoundEnabled,
            settings.VibrationEnabled,
            settings.VoiceEnabled,
            settings.AutoDetectDrivingEnabled,
            settings.AutoStartMonitoringEnabled
        });
    }

    [HttpPut("me/settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateUserSettingsDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var settings = await _db.UserSettings.FirstOrDefaultAsync(item => item.UserId == GetUserId(), cancellationToken);
        if (settings == null)
        {
            return NotFound(new { message = "Settings not found." });
        }

        settings.SpeedUnit = request.SpeedUnit;
        settings.OverspeedTolerance = request.OverspeedTolerance;
        settings.AlertDelaySeconds = request.AlertDelaySeconds;
        settings.AlertCooldownSeconds = request.AlertCooldownSeconds;
        settings.SoundEnabled = request.SoundEnabled;
        settings.VibrationEnabled = request.VibrationEnabled;
        settings.VoiceEnabled = request.VoiceEnabled;
        settings.AutoDetectDrivingEnabled = request.AutoDetectDrivingEnabled;
        settings.AutoStartMonitoringEnabled = request.AutoStartMonitoringEnabled;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Settings updated successfully.",
            settings.SpeedUnit,
            settings.OverspeedTolerance,
            settings.AlertDelaySeconds,
            settings.AlertCooldownSeconds,
            settings.SoundEnabled,
            settings.VibrationEnabled,
            settings.VoiceEnabled,
            settings.AutoDetectDrivingEnabled,
            settings.AutoStartMonitoringEnabled
        });
    }
}

public sealed class UpdateUserSettingsDto
{
    [Required]
    [RegularExpression("^(km/h|mph)$", ErrorMessage = "Speed unit must be 'km/h' or 'mph'.")]
    public string SpeedUnit { get; set; } = "km/h";

    [Range(0, 30)]
    public int OverspeedTolerance { get; set; } = 5;

    [Range(0, 30)]
    public int AlertDelaySeconds { get; set; } = 3;

    [Range(1, 120)]
    public int AlertCooldownSeconds { get; set; } = 10;

    public bool SoundEnabled { get; set; } = true;
    public bool VibrationEnabled { get; set; } = true;
    public bool VoiceEnabled { get; set; }
    public bool AutoDetectDrivingEnabled { get; set; } = true;
    public bool AutoStartMonitoringEnabled { get; set; } = true;
}
