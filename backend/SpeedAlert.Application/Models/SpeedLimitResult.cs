namespace SpeedAlert.Application.Models;

public class SpeedLimitResult
{
    public double? SpeedLimitKph { get; set; }
    public string? RoadName { get; set; }
    public string? SegmentIdentifier { get; set; }
    public string Source { get; set; } = "Unknown";
    public string Status { get; set; } = SpeedLimitLookupStatuses.Unavailable;
    public double Confidence { get; set; }
    public string? ProviderUsed { get; set; }
    public bool FallbackUsed { get; set; }
    public string? Message { get; set; }
    public bool IsCached { get; set; }

    public bool IsSuccessful =>
        SpeedLimitKph.HasValue &&
        (Status == SpeedLimitLookupStatuses.Success || Status == SpeedLimitLookupStatuses.LowConfidence);

    public static SpeedLimitResult Unavailable(string message, string? providerKey = null)
    {
        return new SpeedLimitResult
        {
            ProviderUsed = providerKey,
            Source = providerKey ?? "Unavailable",
            Status = SpeedLimitLookupStatuses.Unavailable,
            Confidence = 0.0,
            Message = message
        };
    }

    public static SpeedLimitResult ProviderFailure(string providerKey, string message)
    {
        return new SpeedLimitResult
        {
            ProviderUsed = providerKey,
            Source = providerKey,
            Status = SpeedLimitLookupStatuses.ProviderFailure,
            Confidence = 0.0,
            Message = message
        };
    }

    public static SpeedLimitResult NotFound(string providerKey, string? roadName = null, string? message = null)
    {
        return new SpeedLimitResult
        {
            ProviderUsed = providerKey,
            Source = providerKey,
            Status = SpeedLimitLookupStatuses.NotFound,
            Confidence = 0.0,
            RoadName = roadName,
            Message = message ?? "Speed limit unavailable for this road segment"
        };
    }
}
