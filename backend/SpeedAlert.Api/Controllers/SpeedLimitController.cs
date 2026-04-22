using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using SpeedAlert.Application.Services;
using SpeedAlert.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/speed-limit")]
[Authorize]
public class SpeedLimitController : ControllerBase
{
    private readonly ISpeedLimitProviderOrchestrator _orchestrator;
    private readonly IAppDbContext _db;

    public SpeedLimitController(ISpeedLimitProviderOrchestrator orchestrator, IAppDbContext db)
    {
        _orchestrator = orchestrator;
        _db = db;
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] double lat, [FromQuery] double lng)
    {
        if (lat == 0 && lng == 0) return BadRequest("Invalid coordinates");

        // Simple rounding to form a rough grid key (approx 100m grid depending on latitude)
        string cacheKey = $"{Math.Round(lat, 3)},{Math.Round(lng, 3)}";

        var cacheEntry = await _db.RoadLookupCaches
            .FirstOrDefaultAsync(c => c.CacheKey == cacheKey);

        if (cacheEntry != null && cacheEntry.ExpiresAt > DateTime.UtcNow)
        {
            return Ok(new 
            {
                speedLimitKph = cacheEntry.SpeedLimitKph,
                source = "PostgreSQL Cache",
                confidence = 1.0,
                providerUsed = (string?)null,
                fallbackUsed = false,
                roadName = cacheEntry.RoadName,
                isCached = true
            });
        }

        var result = await _orchestrator.GetSpeedLimitAsync(lat, lng);
        
        if (result.SpeedLimitKph > 0 && result.Confidence > 0)
        {
            if (cacheEntry == null)
            {
                cacheEntry = new RoadLookupCache
                {
                    CacheKey = cacheKey,
                    RoadName = result.RoadName ?? "Unknown",
                    SpeedLimitKph = result.SpeedLimitKph,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
                _db.RoadLookupCaches.Add(cacheEntry);
            }
            else
            {
                cacheEntry.SpeedLimitKph = result.SpeedLimitKph;
                cacheEntry.ExpiresAt = DateTime.UtcNow.AddDays(7);
                cacheEntry.RetrievedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
        }

        return Ok(new 
        {
            speedLimitKph = result.SpeedLimitKph < 0 ? null : (double?)result.SpeedLimitKph,
            source = result.Source,
            confidence = result.Confidence,
            providerUsed = result.ProviderUsed,
            fallbackUsed = result.FallbackUsed,
            roadName = result.RoadName,
            isCached = false,
            message = result.Message
        });
    }
}
