using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Services;
using SpeedAlert.Domain.Entities;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/monitoring")]
[Authorize(Roles = "User")]
public class DiagnosticsController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ISpeedLimitProviderOrchestrator _orchestrator;

    public DiagnosticsController(IAppDbContext db, ISpeedLimitProviderOrchestrator orchestrator)
    {
        _db = db;
        _orchestrator = orchestrator;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSystemStatus(CancellationToken cancellationToken)
    {
        var databaseStatus = "Disconnected";
        try
        {
            databaseStatus = await ((DbContext)_db).Database.CanConnectAsync(cancellationToken)
                ? "Connected"
                : "Disconnected";
        }
        catch
        {
            databaseStatus = "Disconnected";
        }

        var providerStatuses = await _orchestrator.GetProviderStatusesAsync(cancellationToken);
        var selectedProvider = providerStatuses.FirstOrDefault(status => status.IsSelected);

        var platformHealth =
            databaseStatus == "Connected" &&
            selectedProvider is { IsConfigured: true } &&
            selectedProvider.HealthStatus is "Healthy" or "Degraded"
                ? "Healthy"
                : "Degraded";

        return Ok(new
        {
            ApiVersion = "1.0",
            PlatformHealth = platformHealth,
            DatabaseStatus = databaseStatus,
            SelectedProvider = selectedProvider?.ProviderKey,
            SelectedProviderStatus = selectedProvider?.HealthStatus ?? "Unknown",
            Providers = providerStatuses
        });
    }

    [HttpPost("device-status")]
    public async Task<IActionResult> UploadDeviceStatus([FromBody] DeviceStatusDto status, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { code = "AUTH_UNAUTHORIZED", message = "Authentication is required." });
        }

        var deviceStatus = new DeviceStatus
        {
            UserId = userId,
            Platform = status.Platform.Trim(),
            IsBatteryOptimized = status.IsBatteryOptimized,
            BackgroundLocationGranted = status.BackgroundLocationGranted
        };

        _db.DeviceStatuses.Add(deviceStatus);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok();
    }
}

public sealed class DeviceStatusDto
{
    [Required]
    [MaxLength(32)]
    public string Platform { get; set; } = null!;

    public bool IsBatteryOptimized { get; set; }
    public bool BackgroundLocationGranted { get; set; }
}
