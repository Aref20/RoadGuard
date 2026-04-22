namespace SpeedAlert.Infrastructure.Options;

public sealed class SpeedProvidersOptions
{
    public bool FallbackEnabled { get; set; } = true;

    public ProviderApiOptions Google { get; set; } = new();

    public ProviderApiOptions Here { get; set; } = new();

    public ProviderApiOptions TomTom { get; set; } = new();
}

public sealed class ProviderApiOptions
{
    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }
}
