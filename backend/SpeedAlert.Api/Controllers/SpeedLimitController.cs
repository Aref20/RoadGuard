using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using SpeedAlert.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/speed-limit")]
[Authorize]
public class SpeedLimitController : ControllerBase
{
    private readonly ISpeedLimitProvider _speedLimitProvider;
    private readonly IAppDbContext _db;

    public SpeedLimitController(ISpeedLimitProvider speedLimitProvider, IAppDbContext db)
    {
        _speedLimitProvider = speedLimitProvider;
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
                isCached = true
            });
        }

        var result = await _speedLimitProvider.GetSpeedLimitAsync(lat, lng);
        
        if (result.Source != "Unknown" && result.Source != "ApiErrorFallback" && result.Source != "OfflineFallback")
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
            speedLimitKph = result.SpeedLimitKph,
            source = result.Source,
            confidence = result.Confidence,
            isCached = false
        });
    }
}
