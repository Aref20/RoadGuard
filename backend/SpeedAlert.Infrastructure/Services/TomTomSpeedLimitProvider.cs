using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeedAlert.Infrastructure.Options;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace SpeedAlert.Infrastructure.Services;

public class TomTomSpeedLimitProvider : ISpeedLimitProvider
{
    private readonly HttpClient _http;
    private readonly TomTomSpeedProviderOptions _options;
    private readonly ILogger<TomTomSpeedLimitProvider> _logger;
    private static readonly Regex SpeedLimitRegex = new(@"(?<value>\d+(\.\d+)?)\s*(?<unit>KPH|MPH)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string ProviderKey => SpeedProviderKeys.TomTom;
    public string DisplayName => "TomTom Reverse Geocoding";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public TomTomSpeedLimitProvider(
        HttpClient http,
        IOptions<SpeedProviderOptions> options,
        ILogger<TomTomSpeedLimitProvider> logger)
    {
        _http = http;
        _options = options.Value.TomTom;
        _logger = logger;
    }

    public async Task<SpeedLimitResult> GetSpeedLimitAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("TomTom provider is not configured.");
            return SpeedLimitResult.Unavailable("TomTom API key is not configured.", ProviderKey);
        }

        try
        {
            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/reverseGeocode/{latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}.json" +
                $"?key={Uri.EscapeDataString(_options.ApiKey!)}&returnSpeedLimit=true&radius=100&returnRoadClass=Functional";
            
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var message = response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => "TomTom rate limit reached.",
                    HttpStatusCode.Forbidden => "TomTom request was rejected. Check the API key and subscription.",
                    _ => $"TomTom request failed with HTTP {(int)response.StatusCode}."
                };

                _logger.LogWarning("TomTom lookup failed with status code {StatusCode}.", response.StatusCode);
                return SpeedLimitResult.ProviderFailure(ProviderKey, message);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("addresses", out var addresses) && addresses.GetArrayLength() > 0)
            {
                var match = addresses[0];
                var address = match.GetProperty("address");
                string? roadName = address.TryGetProperty("streetName", out var streetElement)
                    ? streetElement.GetString()
                    : null;
                string? segmentId = match.TryGetProperty("id", out var idElement)
                    ? idElement.GetString()
                    : null;

                double? speedLimit = null;
                if (address.TryGetProperty("speedLimit", out var speedLimitElement))
                {
                    speedLimit = ParseSpeedLimit(speedLimitElement);
                }

                if (!speedLimit.HasValue)
                {
                    _logger.LogInformation("TomTom returned no speed limit for coordinates {Latitude}, {Longitude}.", latitude, longitude);
                    return SpeedLimitResult.NotFound(ProviderKey, roadName, "TomTom returned no posted speed limit for this coordinate.");
                }

                return new SpeedLimitResult
                {
                    SpeedLimitKph = speedLimit,
                    RoadName = roadName,
                    SegmentIdentifier = segmentId,
                    Source = ProviderKey,
                    Status = SpeedLimitLookupStatuses.Success,
                    Confidence = 0.88
                };
            }

            return SpeedLimitResult.NotFound(ProviderKey, message: "TomTom returned no reverse-geocode match.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TomTom lookup failed unexpectedly.");
            return SpeedLimitResult.ProviderFailure(ProviderKey, "TomTom lookup failed unexpectedly.");
        }
    }

    private static double? ParseSpeedLimit(JsonElement speedLimitElement)
    {
        return speedLimitElement.ValueKind switch
        {
            JsonValueKind.Number => speedLimitElement.GetDouble(),
            JsonValueKind.String => ParseSpeedLimitString(speedLimitElement.GetString()),
            _ => null
        };
    }

    private static double? ParseSpeedLimitString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = SpeedLimitRegex.Match(value);
        if (!match.Success || !double.TryParse(match.Groups["value"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue))
        {
            return null;
        }

        var unit = match.Groups["unit"].Value.ToUpperInvariant();
        return unit == "MPH" ? Math.Round(numericValue * 1.60934, 2) : numericValue;
    }
}
