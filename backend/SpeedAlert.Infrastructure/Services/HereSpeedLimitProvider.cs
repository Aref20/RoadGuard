using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using SpeedAlert.Infrastructure.Options;

namespace SpeedAlert.Infrastructure.Services;

public sealed class HereSpeedLimitProvider : ISpeedLimitProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<SpeedProvidersOptions> _optionsMonitor;
    private readonly ILogger<HereSpeedLimitProvider> _logger;

    public HereSpeedLimitProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptionsMonitor<SpeedProvidersOptions> optionsMonitor,
        ILogger<HereSpeedLimitProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public string ProviderKey => SpeedProviderKeys.Here;

    public string DisplayName => "HERE Route Matching";

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
            return SpeedLimitResult.ProviderUnavailable(DisplayName, "HERE API key is not configured.");
        }

        var client = _httpClientFactory.CreateClient();
        var waypointOffset = 0.0001d;
        var waypoint0 = FormattableString.Invariant($"{latitude - waypointOffset},{longitude - waypointOffset}");
        var waypoint1 = FormattableString.Invariant($"{latitude + waypointOffset},{longitude + waypointOffset}");
        var url =
            $"{settings.BaseUrl}?apikey={Uri.EscapeDataString(settings.ApiKey)}" +
            $"&waypoint0={Uri.EscapeDataString(waypoint0)}" +
            $"&waypoint1={Uri.EscapeDataString(waypoint1)}" +
            "&mode=fastest;car;traffic:disabled" +
            "&routeMatch=1" +
            "&attributes=SPEED_LIMITS_FCn(*),ROAD_NAME_FCn(*)";

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "HERE route matching lookup failed with status code {StatusCode}.",
                    response.StatusCode);
                return SpeedLimitResult.Error(DisplayName, $"HERE returned {(int)response.StatusCode}.");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            var speed = FindSpeedLimitKph(document.RootElement);
            if (speed is null or <= 0)
            {
                return SpeedLimitResult.NotFound(DisplayName, "HERE did not return a speed limit for the matched segment.");
            }

            return new SpeedLimitResult
            {
                SpeedLimitKph = speed,
                RoadName = FindFirstString(document.RootElement, "ROAD_NAME", "STREET_NAME", "NAME1"),
                SegmentIdentifier = FindFirstString(document.RootElement, "linkId", "LINK_ID", "segmentRef"),
                ProviderUsed = ProviderKey,
                Source = DisplayName,
                Confidence = 0.88,
                Status = SpeedLimitLookupStatus.Success,
                Message = "Posted speed limit returned by HERE route matching."
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HERE lookup threw an exception.");
            return SpeedLimitResult.Error(DisplayName, "HERE lookup failed unexpectedly.");
        }
    }

    private static double? FindSpeedLimitKph(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var fromLimit = TryGetDouble(element, "FROM_REF_SPEED_LIMIT");
            var toLimit = TryGetDouble(element, "TO_REF_SPEED_LIMIT");
            var speedLimit = fromLimit > 0 ? fromLimit : toLimit;
            if (speedLimit is > 0)
            {
                var unit = TryGetString(element, "SPEED_LIMIT_UNIT");
                return NormalizeHereUnit(speedLimit.Value, unit);
            }

            foreach (var property in element.EnumerateObject())
            {
                var nested = FindSpeedLimitKph(property.Value);
                if (nested is > 0)
                {
                    return nested;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindSpeedLimitKph(item);
                if (nested is > 0)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static double NormalizeHereUnit(double speed, string? unit)
    {
        return unit?.Trim().ToUpperInvariant() switch
        {
            "I" or "MPH" => speed * 1.609344d,
            _ => speed
        };
    }

    private static string? FindFirstString(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                foreach (var propertyName in propertyNames)
                {
                    if (property.NameEquals(propertyName) &&
                        property.Value.ValueKind == JsonValueKind.String)
                    {
                        return property.Value.GetString();
                    }
                }

                var nested = FindFirstString(property.Value, propertyNames);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindFirstString(item, propertyNames);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static double? TryGetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.GetDouble(),
            JsonValueKind.String when double.TryParse(
                property.GetString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var value) => value,
            _ => null
        };
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }
}
