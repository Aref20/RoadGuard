using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedAlert.Application.Services;
using System.Threading.Tasks;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/speed-limit")]
[Authorize(Roles = "User")]
public class SpeedLimitController : ControllerBase
{
    private readonly ISpeedLimitProviderOrchestrator _orchestrator;

    public SpeedLimitController(ISpeedLimitProviderOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] double lat, [FromQuery] double lng, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.GetSpeedLimitAsync(lat, lng, cancellationToken);
        return Ok(new
        {
            speedLimitKph = result.SpeedLimitKph,
            source = result.Source,
            status = result.Status,
            confidence = result.Confidence,
            providerUsed = result.ProviderUsed,
            fallbackUsed = result.FallbackUsed,
            roadName = result.RoadName,
            segmentIdentifier = result.SegmentIdentifier,
            isCached = result.IsCached,
            message = result.Message
        });
    }
}
