namespace SpeedAlert.Infrastructure.Options;

public sealed class SpeedProviderOptions
{
    public bool FallbackEnabled { get; set; } = true;
    public GoogleSpeedProviderOptions Google { get; set; } = new();
    public HereSpeedProviderOptions Here { get; set; } = new();
    public TomTomSpeedProviderOptions TomTom { get; set; } = new();
}

public sealed class GoogleSpeedProviderOptions
{
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://roads.googleapis.com/v1/";
}

public sealed class HereSpeedProviderOptions
{
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://router.hereapi.com/v8/";
}

public sealed class TomTomSpeedProviderOptions
{
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.tomtom.com/search/2/";
}
