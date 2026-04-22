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
using System.Threading;

namespace SpeedAlert.Infrastructure.Services;

public class HereSpeedLimitProvider : ISpeedLimitProvider
{
    private readonly HttpClient _http;
    private readonly HereSpeedProviderOptions _options;
    private readonly ILogger<HereSpeedLimitProvider> _logger;

    public string ProviderKey => SpeedProviderKeys.Here;
    public string DisplayName => "HERE Routing";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public HereSpeedLimitProvider(
        HttpClient http,
        IOptions<SpeedProviderOptions> options,
        ILogger<HereSpeedLimitProvider> logger)
    {
        _http = http;
        _options = options.Value.Here;
        _logger = logger;
    }

    public async Task<SpeedLimitResult> GetSpeedLimitAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("HERE provider is not configured.");
            return SpeedLimitResult.Unavailable("HERE API key is not configured.", ProviderKey);
        }

        try
        {
            var destinationLatitude = latitude + 0.0003d;
            var destinationLongitude = longitude + 0.0003d;
            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/routes" +
                $"?transportMode=car" +
                $"&origin={latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&destination={destinationLatitude.ToString(CultureInfo.InvariantCulture)},{destinationLongitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&return=polyline" +
                $"&spans=maxSpeed,names,segmentRef" +
                $"&apiKey={Uri.EscapeDataString(_options.ApiKey!)}";
            
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var message = response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => "HERE rate limit reached.",
                    HttpStatusCode.Forbidden => "HERE request was rejected. Check the API key and subscription.",
                    _ => $"HERE request failed with HTTP {(int)response.StatusCode}."
                };

                _logger.LogWarning("HERE lookup failed with status code {StatusCode}.", response.StatusCode);
                return SpeedLimitResult.ProviderFailure(ProviderKey, message);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
            {
                var route = routes[0];
                if (route.TryGetProperty("sections", out var sections) && sections.GetArrayLength() > 0)
                {
                    var section = sections[0];
                    if (section.TryGetProperty("spans", out var spans) && spans.GetArrayLength() > 0)
                    {
                        foreach (var span in spans.EnumerateArray())
                        {
                            var speedLimit = TryReadSpeed(span, "maxSpeed") ?? TryReadSpeed(span, "speedLimit");
                            if (!speedLimit.HasValue)
                            {
                                continue;
                            }

                            var roadName = TryReadName(span);
                            var segmentRef = span.TryGetProperty("segmentRef", out var segmentRefElement)
                                ? segmentRefElement.ToString()
                                : null;

                            return new SpeedLimitResult
                            {
                                SpeedLimitKph = speedLimit,
                                RoadName = roadName,
                                SegmentIdentifier = segmentRef,
                                Source = ProviderKey,
                                Status = speedLimit.Value < 15
                                    ? SpeedLimitLookupStatuses.LowConfidence
                                    : SpeedLimitLookupStatuses.Success,
                                Confidence = speedLimit.Value < 15 ? 0.45 : 0.9,
                                Message = speedLimit.Value < 15
                                    ? "HERE returned an unusually low maxSpeed value."
                                    : null
                            };
                        }
                    }
                }
            }

            _logger.LogInformation("HERE returned no speed metadata for coordinates {Latitude}, {Longitude}.", latitude, longitude);
            return SpeedLimitResult.NotFound(ProviderKey, message: "HERE returned no speed metadata for this coordinate.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HERE lookup failed unexpectedly.");
            return SpeedLimitResult.ProviderFailure(ProviderKey, "HERE lookup failed unexpectedly.");
        }
    }

    private static double? TryReadSpeed(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var speedElement))
        {
            return null;
        }

        return speedElement.ValueKind switch
        {
            JsonValueKind.Number => speedElement.GetDouble(),
            JsonValueKind.String when double.TryParse(speedElement.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue) => numericValue,
            _ => null
        };
    }

    private static string? TryReadName(JsonElement span)
    {
        if (!span.TryGetProperty("names", out var namesElement) || namesElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in namesElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                return item.GetString();
            }

            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("value", out var valueElement))
            {
                return valueElement.GetString();
            }
        }

        return null;
    }
}
