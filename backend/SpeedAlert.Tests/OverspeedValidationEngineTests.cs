using System;
using System.Collections.Generic;
using SpeedAlert.Domain.Entities;
using SpeedAlert.Domain.Services;
using Xunit;
using FluentAssertions;

namespace SpeedAlert.Tests;

public class OverspeedValidationEngineTests
{
    private readonly OverspeedValidationEngine _engine;

    public OverspeedValidationEngineTests()
    {
        _engine = new OverspeedValidationEngine();
    }

    [Fact]
    public void IsViolationConfirmed_ReturnsFalse_WhenPointsAreInsufficient()
    {
        var points = new List<SessionPoint>
        {
            new SessionPoint { SpeedKph = 70, IsValid = true, AccuracyMeters = 10, Timestamp = DateTime.UtcNow }
        };

        var result = _engine.IsViolationConfirmed(points, 60, 5, 3);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsViolationConfirmed_ReturnsFalse_WhenSpeedIsUnderTolerance()
    {
        var startTime = DateTime.UtcNow;
        var points = new List<SessionPoint>
        {
            new SessionPoint { SpeedKph = 62, IsValid = true, AccuracyMeters = 10, Timestamp = startTime },
            new SessionPoint { SpeedKph = 64, IsValid = true, AccuracyMeters = 10, Timestamp = startTime.AddSeconds(5) }
        };

        // Limit = 60, Tolerance = 5 (Allowed up to 65)
        var result = _engine.IsViolationConfirmed(points, 60, 5, 3);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsViolationConfirmed_ReturnsTrue_WhenSpeedConsistentlyOverToleranceAndDurationMet()
    {
        var startTime = DateTime.UtcNow;
        var points = new List<SessionPoint>
        {
            new SessionPoint { SpeedKph = 68, IsValid = true, AccuracyMeters = 10, Timestamp = startTime },
            new SessionPoint { SpeedKph = 70, IsValid = true, AccuracyMeters = 10, Timestamp = startTime.AddSeconds(4) }
        };

        // Limit = 60, Tolerance = 5 (Trigger over 65). Duration required = 3.
        var result = _engine.IsViolationConfirmed(points, 60, 5, 3);
        result.Should().BeTrue();
    }
}
