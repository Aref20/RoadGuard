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

    [HttpGet]
    public async Task<IActionResult> GetMySessions()
    {
        var sessions = await _db.Sessions
            .Where(s => s.UserId == GetUserId())
            .OrderByDescending(s => s.StartedAt)
            .Take(50)
            .Select(s => new { 
                s.Id, 
                s.StartedAt, 
                s.EndedAt, 
                s.Status,
                s.WasAutoStarted,
                s.OverspeedEventCount,
                s.AlertEventCount,
                s.TotalDistanceMeters 
            })
            .ToListAsync();
        return Ok(sessions);
    }

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
    [HttpPost("{id}/alerts")]
    public async Task<IActionResult> UploadAlerts(Guid id, [FromBody] List<AlertDto> alerts)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == GetUserId());
        if (session == null) return NotFound();

        var dbAlerts = alerts.Select(a => new AlertEvent
        {
            DrivingSessionId = id,
            Timestamp = a.Timestamp,
            AlertType = a.AlertType,
            ActualSpeedKph = a.ActualSpeed,
            SpeedLimitKph = a.SpeedLimit
        });
        
        session.AlertEventCount += alerts.Count;
        
        // Count as overspeed if type is Overspeed
        var overspeedCount = alerts.Count(a => a.AlertType == "Overspeed");
        session.OverspeedEventCount += overspeedCount;
        
        foreach (var a in alerts) 
        {
            if (a.ActualSpeed > session.MostSevereOverspeedKph) {
                session.MostSevereOverspeedKph = a.ActualSpeed;
            }
        }

        _db.AlertEvents.AddRange(dbAlerts);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id}/end")]
    public async Task<IActionResult> EndSession(Guid id, [FromBody] EndSessionDto dto)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == GetUserId());
        if (session == null) return NotFound();

        session.Status = "Ended";
        session.EndedAt = DateTime.UtcNow;
        session.SessionEndReason = dto.Reason;
        
        // Simple mock calculations since we don't recalculate entire track here
        session.TotalDistanceMeters = dto.DistanceMeters;

        await _db.SaveChangesAsync();
        return Ok();
    }
}

public class StartSessionDto { public string Reason { get; set; } = "manual"; public bool IsAuto { get; set; } }
public class EndSessionDto { public string Reason { get; set; } = "manual"; public double DistanceMeters { get; set; } }
public class PointDto { public DateTime Timestamp { get; set; } public double Lat { get; set; } public double Lng { get; set; } public double Speed { get; set; } public double Accuracy { get; set; } }
public class AlertDto { public DateTime Timestamp { get; set; } public string AlertType { get; set; } = "Overspeed"; public double ActualSpeed { get; set; } public double SpeedLimit { get; set; } }
