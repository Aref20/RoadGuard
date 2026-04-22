using System;
using System.Collections.Generic;

namespace SpeedAlert.Application.Models;

public static class SpeedProviderKeys
{
    public const string Google = "Google";
    public const string Here = "Here";
    public const string TomTom = "TomTom";

    public static readonly string[] All = [Google, Here, TomTom];
}

public static class SpeedProviderHealthStatuses
{
    public const string Healthy = "Healthy";
    public const string Degraded = "Degraded";
    public const string NotConfigured = "NotConfigured";
    public const string Disabled = "Disabled";
    public const string Unknown = "Unknown";
}

public sealed class SpeedProviderStatusDto
{
    public string ProviderKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public bool IsSelected { get; set; }

    public int PriorityOrder { get; set; }

    public bool IsConfigured { get; set; }

    public string HealthStatus { get; set; } = SpeedProviderHealthStatuses.Unknown;

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastSuccessfulLookupAt { get; set; }

    public DateTime? LastFailureAt { get; set; }

    public string? LastFailureReason { get; set; }
}

public sealed class SpeedProviderSettingsResponse
{
    public bool FallbackEnabled { get; set; } = true;

    public IReadOnlyList<SpeedProviderStatusDto> Providers { get; set; } = [];
}

public sealed class ProviderConfigUpdateDto
{
    public string ProviderKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public bool IsSelected { get; set; }

    public int PriorityOrder { get; set; }
}

public sealed class UpdateProviderSettingsRequest
{
    public bool FallbackEnabled { get; set; } = true;

    public List<ProviderConfigUpdateDto> Providers { get; set; } = [];
}

public sealed class ProviderTestRequest
{
    public string ProviderKey { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
