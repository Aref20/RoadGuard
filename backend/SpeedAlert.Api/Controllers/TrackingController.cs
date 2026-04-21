using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Domain.Entities;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly IAppDbContext _db;
    
    public TrackingController(IAppDbContext db) => _db = db;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionDto dto)
    {
        var session = new DrivingSession
        {
            UserId = GetUserId(),
            SessionStartReason = dto.Reason,
            WasAutoStarted = dto.IsAuto
        };
        
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        
        return Ok(new { SessionId = session.Id });
    }

    [HttpPost("{id}/points")]
    public async Task<IActionResult> UploadPoints(Guid id, [FromBody] List<PointDto> points)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == GetUserId());
        if (session == null) return NotFound();

        var dbPoints = points.Select(p => new SessionPoint
        {
            DrivingSessionId = id,
            Timestamp = p.Timestamp,
            Latitude = p.Lat,
            Longitude = p.Lng,
            SpeedKph = p.Speed,
            AccuracyMeters = p.Accuracy
        });
        
        _db.SessionPoints.AddRange(dbPoints);
        await _db.SaveChangesAsync();
        return Ok();
    }
}

public class StartSessionDto { public string Reason { get; set; } = "manual"; public bool IsAuto { get; set; } }
public class PointDto { public DateTime Timestamp { get; set; } public double Lat { get; set; } public double Lng { get; set; } public double Speed { get; set; } public double Accuracy { get; set; } }
