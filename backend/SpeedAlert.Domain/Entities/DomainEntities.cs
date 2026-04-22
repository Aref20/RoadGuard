using System;
using System.Collections.Generic;

namespace SpeedAlert.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    public UserSettings Settings { get; set; } = null!;
    public ICollection<DrivingSession> Sessions { get; set; } = new List<DrivingSession>();
}

public class UserSettings
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string SpeedUnit { get; set; } = "km/h"; // km/h or mph
    public int OverspeedTolerance { get; set; } = 5;
    public int AlertDelaySeconds { get; set; } = 3;
    public int AlertCooldownSeconds { get; set; } = 10;
    
    public bool SoundEnabled { get; set; } = true;
    public bool VibrationEnabled { get; set; } = true;
    public bool VoiceEnabled { get; set; } = false;
    
    public bool AutoDetectDrivingEnabled { get; set; } = true;
    public bool AutoStartMonitoringEnabled { get; set; } = true;
}

public class DrivingSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = "Active"; // Active, Ended, Paused
    
    public double TotalDistanceMeters { get; set; }
    public double AverageSpeedKph { get; set; }
    public double MaxSpeedKph { get; set; }
    public int OverspeedEventCount { get; set; }
    public int AlertEventCount { get; set; }
    public double MostSevereOverspeedKph { get; set; }
    
    public string SessionStartReason { get; set; } = "manual";
    public string SessionEndReason { get; set; } = "unknown";
    public bool WasAutoStarted { get; set; }

    public ICollection<SessionPoint> Points { get; set; } = new List<SessionPoint>();
    public ICollection<AlertEvent> Alerts { get; set; } = new List<AlertEvent>();
}

public class SessionPoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DrivingSessionId { get; set; }
    public DrivingSession DrivingSession { get; set; } = null!;
    
    public DateTime Timestamp { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double SpeedKph { get; set; }
    public double AccuracyMeters { get; set; }
    public bool IsValid { get; set; } = true;
}

public class AlertEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DrivingSessionId { get; set; }
    public DrivingSession DrivingSession { get; set; } = null!;
    
    public DateTime Timestamp { get; set; }
    public string AlertType { get; set; } = "Overspeed";
    public double ActualSpeedKph { get; set; }
    public double SpeedLimitKph { get; set; }
}

public class DeviceStatus
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Platform { get; set; } = null!;
    public bool IsBatteryOptimized { get; set; }
    public bool BackgroundLocationGranted { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class RoadLookupCache
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CacheKey { get; set; } = null!;
    public string StrategyKey { get; set; } = null!;
    public string RoadName { get; set; } = "Unknown";
    public double SpeedLimitKph { get; set; }
    public string ProviderKey { get; set; } = null!;
    public string Source { get; set; } = "Unknown";
    public string? SegmentIdentifier { get; set; }
    public double Confidence { get; set; }
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}

public class ProviderConfig
{
    public string ProviderKey { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;
    public int PriorityOrder { get; set; } = 0;
    public bool IsSelected { get; set; } = false;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? LastSuccessfulLookupAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public string? LastFailureReason { get; set; }
    public string LastHealthStatus { get; set; } = "Unknown";
}

public class ProviderRuntimeSettings
{
    public int Id { get; set; } = 1;
    public bool FallbackEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
