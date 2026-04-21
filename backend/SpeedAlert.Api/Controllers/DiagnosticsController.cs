using Microsoft.AspNetCore.Mvc;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/monitoring")]
public class DiagnosticsController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetSystemStatus()
    {
        return Ok(new {
            ApiVersion = "1.0",
            PlatformHealth = "Healthy",
            SpeedLimitProviderStatus = "Active" // This would ping the actual provider logically
        });
    }

    [HttpPost("device-status")]
    public IActionResult UploadDeviceStatus([FromBody] DeviceStatusDto status)
    {
        // This accepts telemetry from mobile explaining if it's restricted by Battery Savers
        return Ok();
    }
}

public class DeviceStatusDto { 
    public string Platform { get; set; } = null!;
    public bool IsBatteryOptimized { get; set; } 
    public bool BackgroundLocationGranted { get; set; }
}
