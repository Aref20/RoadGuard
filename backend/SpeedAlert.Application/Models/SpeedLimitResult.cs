namespace SpeedAlert.Application.Models;

public enum SpeedLimitLookupStatus
{
    Success,
    NotFound,
    ProviderUnavailable,
    Error,
    Unknown
}

public sealed class SpeedLimitResult
{
    public double? SpeedLimitKph { get; set; }

    public string? RoadName { get; set; }

    public string Source { get; set; } = "Unavailable";

    public double Confidence { get; set; }

    public string? ProviderUsed { get; set; }

    public bool FallbackUsed { get; set; }

    public string? Message { get; set; }

    public bool IsCached { get; set; }

    public string? SegmentIdentifier { get; set; }

    public SpeedLimitLookupStatus Status { get; set; } = SpeedLimitLookupStatus.Unknown;

    public bool IsTrustworthy =>
        Status == SpeedLimitLookupStatus.Success &&
        SpeedLimitKph is > 0 &&
        Confidence > 0;

    public static SpeedLimitResult ProviderUnavailable(string source, string message) =>
        new()
        {
            Source = source,
            Message = message,
            Status = SpeedLimitLookupStatus.ProviderUnavailable,
            Confidence = 0
        };

    public static SpeedLimitResult NotFound(string source, string? message = null) =>
        new()
        {
            Source = source,
            Message = message ?? "Speed limit unavailable",
            Status = SpeedLimitLookupStatus.NotFound,
            Confidence = 0
        };

    public static SpeedLimitResult Error(string source, string message) =>
        new()
        {
            Source = source,
            Message = message,
            Status = SpeedLimitLookupStatus.Error,
            Confidence = 0
        };
}
