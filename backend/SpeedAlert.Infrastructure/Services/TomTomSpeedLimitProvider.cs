using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using SpeedAlert.Infrastructure.Options;

namespace SpeedAlert.Infrastructure.Services;

public sealed class TomTomSpeedLimitProvider : ISpeedLimitProvider
{
    private static readonly Regex SpeedLimitPattern =
        new(@"(?<value>\d+(\.\d+)?)\s*(?<unit>KPH|MPH)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<SpeedProvidersOptions> _optionsMonitor;
    private readonly ILogger<TomTomSpeedLimitProvider> _logger;

    public TomTomSpeedLimitProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptionsMonitor<SpeedProvidersOptions> optionsMonitor,
        ILogger<TomTomSpeedLimitProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public string ProviderKey => SpeedProviderKeys.TomTom;

    public string DisplayName => "TomTom Reverse Geocode";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(
            SpeedProviderConfiguration.Resolve(ProviderKey, _optionsMonitor.CurrentValue, _configuration).ApiKey);

    public async Task<SpeedLimitResult> GetSpeedLimitAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var settings = SpeedProviderConfiguration.Resolve(ProviderKey, _optionsMonitor.CurrentValue, _configuration);
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return SpeedLimitResult.ProviderUnavailable(DisplayName, "TomTom API key is not configured.");
        }

        var client = _httpClientFactory.CreateClient();
        var coordinate = FormattableString.Invariant($"{latitude},{longitude}");
        var url =
            $"{settings.BaseUrl?.TrimEnd('/')}/{Uri.EscapeDataString(coordinate)}.json" +
            $"?key={Uri.EscapeDataString(settings.ApiKey)}" +
            "&returnSpeedLimit=true";

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "TomTom reverse geocode lookup failed with status code {StatusCode}.",
                    response.StatusCode);
                return SpeedLimitResult.Error(DisplayName, $"TomTom returned {(int)response.StatusCode}.");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("addresses", out var addresses) ||
                addresses.GetArrayLength() == 0)
            {
                return SpeedLimitResult.NotFound(DisplayName, "TomTom did not return a matched address.");
            }

            var addressEntry = addresses[0];
            if (!addressEntry.TryGetProperty("address", out var address))
            {
                return SpeedLimitResult.NotFound(DisplayName, "TomTom did not return an address payload.");
            }

            var parsedSpeedLimit = ParseSpeedLimit(address.TryGetProperty("speedLimit", out var speedLimitProperty)
                ? speedLimitProperty.GetString()
                : null);

            if (parsedSpeedLimit is null or <= 0)
            {
                return SpeedLimitResult.NotFound(DisplayName, "TomTom did not return a posted speed limit.");
            }

            var roadName = address.TryGetProperty("streetName", out var streetName)
                ? streetName.GetString()
                : null;

            var segmentIdentifier = address.TryGetProperty("streetNameAndNumber", out var streetNameAndNumber)
                ? streetNameAndNumber.GetString()
                : roadName;

            return new SpeedLimitResult
            {
                SpeedLimitKph = parsedSpeedLimit,
                RoadName = roadName,
                SegmentIdentifier = segmentIdentifier,
                ProviderUsed = ProviderKey,
                Source = DisplayName,
                Confidence = 0.84,
                Status = SpeedLimitLookupStatus.Success,
                Message = "Posted speed limit returned by TomTom reverse geocode."
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TomTom lookup threw an exception.");
            return SpeedLimitResult.Error(DisplayName, "TomTom lookup failed unexpectedly.");
        }
    }

    private static double? ParseSpeedLimit(string? speedLimit)
    {
        if (string.IsNullOrWhiteSpace(speedLimit))
        {
            return null;
        }

        var match = SpeedLimitPattern.Match(speedLimit);
        if (!match.Success)
        {
            return null;
        }

        if (!double.TryParse(match.Groups["value"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        var unit = match.Groups["unit"].Value.ToUpperInvariant();
        return unit == "MPH" ? value * 1.609344d : value;
    }
}
