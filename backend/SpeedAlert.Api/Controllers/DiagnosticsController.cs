using Microsoft.AspNetCore.Mvc;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Domain.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/monitoring")]
[Authorize]
public class DiagnosticsController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ISpeedLimitProvider _speedLimitProvider;

    public DiagnosticsController(IAppDbContext db, ISpeedLimitProvider speedLimitProvider)
    {
        _db = db;
        _speedLimitProvider = speedLimitProvider;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSystemStatus()
    {
        bool dbHealthy = false;
        try {
            dbHealthy = await ((Microsoft.EntityFrameworkCore.DbContext)_db).Database.CanConnectAsync();
        } catch { }

        // Test Provider with a dummy coordinate
        bool providerHealthy = false;
        try {
            var result = await _speedLimitProvider.GetSpeedLimitAsync(0, 0);
            providerHealthy = true; // If it doesn't throw, we assume it's capable
        } catch { }

        return Ok(new {
            ApiVersion = "1.0",
            PlatformHealth = dbHealthy && providerHealthy ? "Healthy" : "Degraded",
            DatabaseStatus = dbHealthy ? "Connected" : "Disconnected",
            SpeedLimitProviderStatus = providerHealthy ? "Active" : "Offline"
        });
    }

    [HttpPost("device-status")]
    public async Task<IActionResult> UploadDeviceStatus([FromBody] DeviceStatusDto status)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!System.Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        var deviceStatus = new DeviceStatus
        {
            UserId = userId,
            Platform = status.Platform,
            IsBatteryOptimized = status.IsBatteryOptimized,
            BackgroundLocationGranted = status.BackgroundLocationGranted
        };

        _db.DeviceStatuses.Add(deviceStatus);
        await _db.SaveChangesAsync();

        return Ok();
    }
}

public class DeviceStatusDto { 
    public string Platform { get; set; } = null!;
    public bool IsBatteryOptimized { get; set; } 
    public bool BackgroundLocationGranted { get; set; }
}
