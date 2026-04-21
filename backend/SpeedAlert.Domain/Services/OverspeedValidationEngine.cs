using System.Collections.Generic;
using System.Linq;
using SpeedAlert.Domain.Entities;

namespace SpeedAlert.Domain.Services;

public class OverspeedValidationEngine
{
    /// <summary>
    /// Smooths location samples and determines if a violation occurred.
    /// This validates the client's internal alerts against the backend truth.
    /// </summary>
    public bool IsViolationConfirmed(List<SessionPoint> recentPoints, double speedLimitKph, int toleranceKph, int requiredDurationSeconds)
    {
        if (recentPoints == null || recentPoints.Count < 2) return false;

        // Strip outliers (e.g. impossible acceleration)
        var validPoints = recentPoints.Where(p => p.IsValid && p.AccuracyMeters < 50).OrderBy(p => p.Timestamp).ToList();
        if (validPoints.Count < 2) return false;

        var avgSpeed = validPoints.Average(p => p.SpeedKph);
        if (avgSpeed <= (speedLimitKph + toleranceKph)) return false;

        var duration = (validPoints.Last().Timestamp - validPoints.First().Timestamp).TotalSeconds;
        return duration >= requiredDurationSeconds;
    }
}
