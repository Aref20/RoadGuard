using System;

namespace SpeedAlert.Application.Models;

public sealed class SpeedLimitProviderStatus
{
    public string ProviderKey { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public bool IsSelected { get; set; }
    public int PriorityOrder { get; set; }
    public bool IsConfigured { get; set; }
    public string HealthStatus { get; set; } = "Unknown";
    public string? LastFailureReason { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
