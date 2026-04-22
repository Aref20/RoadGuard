using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Domain.Entities;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize(Roles = "User")]
public class TrackingController : ControllerBase
{
    private const int MaxPointBatchSize = 100;
    private const int MaxAlertBatchSize = 50;
    private readonly IAppDbContext _db;

    public TrackingController(IAppDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMySessions(CancellationToken cancellationToken)
    {
        var sessions = await _db.Sessions
            .Where(session => session.UserId == GetUserId())
            .OrderByDescending(session => session.StartedAt)
            .Take(50)
            .Select(session => new
            {
                session.Id,
                session.StartedAt,
                session.EndedAt,
                session.Status,
                session.WasAutoStarted,
                session.OverspeedEventCount,
                session.AlertEventCount,
                session.TotalDistanceMeters,
                session.AverageSpeedKph,
                session.MaxSpeedKph
            })
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = GetUserId();
        var activeSession = await _db.Sessions
            .FirstOrDefaultAsync(session => session.UserId == userId && session.Status == "Active", cancellationToken);

        if (activeSession != null)
        {
            return Ok(new { sessionId = activeSession.Id, reusedExisting = true });
        }

        var session = new DrivingSession
        {
            UserId = userId,
            SessionStartReason = dto.Reason.Trim(),
            WasAutoStarted = dto.IsAuto,
            Status = "Active",
            StartedAt = DateTime.UtcNow
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { sessionId = session.Id, reusedExisting = false });
    }

    [HttpPost("{id:guid}/points")]
    public async Task<IActionResult> UploadPoints(Guid id, [FromBody] List<PointDto> points, CancellationToken cancellationToken)
    {
        if (points.Count == 0)
        {
            return BadRequest(new { code = "POINT_BATCH_EMPTY", message = "At least one point is required." });
        }

        if (points.Count > MaxPointBatchSize)
        {
            return BadRequest(new { code = "POINT_BATCH_TOO_LARGE", message = $"Point batch cannot exceed {MaxPointBatchSize} items." });
        }

        var session = await LoadOwnedSessionAsync(id, requireActive: true, cancellationToken);
        if (session == null)
        {
            return NotFound(new { message = "Active session not found." });
        }

        var orderedPoints = points.OrderBy(point => point.Timestamp).ToList();
        foreach (var point in orderedPoints)
        {
            if (!IsValidPoint(point))
            {
                return BadRequest(new { code = "POINT_BATCH_INVALID", message = "One or more points are invalid." });
            }
        }

        var lastExistingPoint = await _db.SessionPoints
            .Where(point => point.DrivingSessionId == id)
            .OrderByDescending(point => point.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var dbPoints = orderedPoints.Select(point => new SessionPoint
        {
            DrivingSessionId = id,
            Timestamp = point.Timestamp,
            Latitude = point.Lat,
            Longitude = point.Lng,
            SpeedKph = point.Speed,
            AccuracyMeters = point.Accuracy,
            IsValid = point.Accuracy <= 100
        }).ToList();

        _db.SessionPoints.AddRange(dbPoints);

        var distanceIncrement = CalculateDistanceIncrement(lastExistingPoint, dbPoints);
        session.TotalDistanceMeters += distanceIncrement;
        session.MaxSpeedKph = Math.Max(session.MaxSpeedKph, dbPoints.Max(point => point.SpeedKph));

        await _db.SaveChangesAsync(cancellationToken);

        session.AverageSpeedKph = await _db.SessionPoints
            .Where(point => point.DrivingSessionId == id)
            .AverageAsync(point => point.SpeedKph, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { accepted = dbPoints.Count, distanceIncrementMeters = Math.Round(distanceIncrement, 2) });
    }

    [HttpPost("{id:guid}/alerts")]
    public async Task<IActionResult> UploadAlerts(Guid id, [FromBody] List<AlertDto> alerts, CancellationToken cancellationToken)
    {
        if (alerts.Count == 0)
        {
            return BadRequest(new { code = "ALERT_BATCH_EMPTY", message = "At least one alert is required." });
        }

        if (alerts.Count > MaxAlertBatchSize)
        {
            return BadRequest(new { code = "ALERT_BATCH_TOO_LARGE", message = $"Alert batch cannot exceed {MaxAlertBatchSize} items." });
        }

        var session = await LoadOwnedSessionAsync(id, requireActive: true, cancellationToken);
        if (session == null)
        {
            return NotFound(new { message = "Active session not found." });
        }

        foreach (var alert in alerts)
        {
            if (!IsValidAlert(alert))
            {
                return BadRequest(new { code = "ALERT_BATCH_INVALID", message = "One or more alerts are invalid." });
            }
        }

        var dbAlerts = alerts.Select(alert => new AlertEvent
        {
            DrivingSessionId = id,
            Timestamp = alert.Timestamp,
            AlertType = alert.AlertType.Trim(),
            ActualSpeedKph = alert.ActualSpeed,
            SpeedLimitKph = alert.SpeedLimit
        }).ToList();

        session.AlertEventCount += dbAlerts.Count;
        session.OverspeedEventCount += dbAlerts.Count(alert => string.Equals(alert.AlertType, "Overspeed", StringComparison.OrdinalIgnoreCase));

        foreach (var alert in dbAlerts)
        {
            var overspeedAmount = Math.Max(0, alert.ActualSpeedKph - alert.SpeedLimitKph);
            session.MostSevereOverspeedKph = Math.Max(session.MostSevereOverspeedKph, overspeedAmount);
        }

        _db.AlertEvents.AddRange(dbAlerts);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { accepted = dbAlerts.Count });
    }

    [HttpPut("{id:guid}/end")]
    public async Task<IActionResult> EndSession(Guid id, [FromBody] EndSessionDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var session = await _db.Sessions
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == GetUserId(), cancellationToken);
        if (session == null)
        {
            return NotFound(new { message = "Session not found." });
        }

        if (session.Status != "Active")
        {
            return Ok(new { sessionId = session.Id, alreadyEnded = true });
        }

        var points = await _db.SessionPoints
            .Where(point => point.DrivingSessionId == id)
            .OrderBy(point => point.Timestamp)
            .ToListAsync(cancellationToken);

        session.Status = "Ended";
        session.EndedAt = DateTime.UtcNow;
        session.SessionEndReason = dto.Reason.Trim();
        session.TotalDistanceMeters = CalculateDistance(points);
        session.MaxSpeedKph = points.Count > 0 ? points.Max(point => point.SpeedKph) : session.MaxSpeedKph;
        session.AverageSpeedKph = points.Count > 0 ? points.Average(point => point.SpeedKph) : session.AverageSpeedKph;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { sessionId = session.Id, alreadyEnded = false, totalDistanceMeters = session.TotalDistanceMeters });
    }

    private async Task<DrivingSession?> LoadOwnedSessionAsync(Guid sessionId, bool requireActive, CancellationToken cancellationToken)
    {
        return await _db.Sessions.FirstOrDefaultAsync(
            session => session.Id == sessionId &&
                       session.UserId == GetUserId() &&
                       (!requireActive || session.Status == "Active"),
            cancellationToken);
    }

    private static bool IsValidPoint(PointDto point)
    {
        return point.Lat is >= -90 and <= 90 &&
               point.Lng is >= -180 and <= 180 &&
               point.Speed is >= 0 and <= 350 &&
               point.Accuracy is >= 0 and <= 500 &&
               point.Timestamp > DateTime.UtcNow.AddDays(-7) &&
               point.Timestamp < DateTime.UtcNow.AddMinutes(5);
    }

    private static bool IsValidAlert(AlertDto alert)
    {
        return !string.IsNullOrWhiteSpace(alert.AlertType) &&
               alert.AlertType.Length <= 64 &&
               alert.ActualSpeed is >= 0 and <= 350 &&
               alert.SpeedLimit is >= 0 and <= 250 &&
               alert.Timestamp > DateTime.UtcNow.AddDays(-7) &&
               alert.Timestamp < DateTime.UtcNow.AddMinutes(5);
    }

    private static double CalculateDistanceIncrement(SessionPoint? lastExistingPoint, IReadOnlyList<SessionPoint> newPoints)
    {
        var points = new List<SessionPoint>();
        if (lastExistingPoint != null)
        {
            points.Add(lastExistingPoint);
        }

        points.AddRange(newPoints);
        return CalculateDistance(points);
    }

    private static double CalculateDistance(IReadOnlyList<SessionPoint> points)
    {
        if (points.Count < 2)
        {
            return 0;
        }

        double totalDistanceMeters = 0;
        for (var index = 1; index < points.Count; index++)
        {
            totalDistanceMeters += HaversineDistanceMeters(points[index - 1], points[index]);
        }

        return totalDistanceMeters;
    }

    private static double HaversineDistanceMeters(SessionPoint previous, SessionPoint current)
    {
        const double earthRadiusMeters = 6371000;
        double dLat = DegreesToRadians(current.Latitude - previous.Latitude);
        double dLon = DegreesToRadians(current.Longitude - previous.Longitude);

        var lat1 = DegreesToRadians(previous.Latitude);
        var lat2 = DegreesToRadians(current.Latitude);

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}

public sealed class StartSessionDto
{
    [Required]
    [MaxLength(64)]
    public string Reason { get; set; } = "manual";

    public bool IsAuto { get; set; }
}

public sealed class EndSessionDto
{
    [Required]
    [MaxLength(64)]
    public string Reason { get; set; } = "manual";
}

public sealed class PointDto
{
    public DateTime Timestamp { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Speed { get; set; }
    public double Accuracy { get; set; }
}

public sealed class AlertDto
{
    public DateTime Timestamp { get; set; }
    public string AlertType { get; set; } = "Overspeed";
    public double ActualSpeed { get; set; }
    public double SpeedLimit { get; set; }
}
