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

    public DiagnosticsController(IAppDbContext db) => _db = db;

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetSystemStatus()
    {
        return Ok(new {
            ApiVersion = "1.0",
            PlatformHealth = "Healthy",
            SpeedLimitProviderStatus = "Active" // This would ping the actual provider logically
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
