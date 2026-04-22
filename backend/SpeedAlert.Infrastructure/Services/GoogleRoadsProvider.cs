using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeedAlert.Infrastructure.Options;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Net;
using System.Threading;

namespace SpeedAlert.Infrastructure.Services;

public class GoogleRoadsProvider : ISpeedLimitProvider
{
    private readonly HttpClient _http;
    private readonly GoogleSpeedProviderOptions _options;
    private readonly ILogger<GoogleRoadsProvider> _logger;

    public string ProviderKey => SpeedProviderKeys.Google;
    public string DisplayName => "Google Roads";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);
    
    public GoogleRoadsProvider(
        HttpClient http,
        IOptions<SpeedProviderOptions> options,
        ILogger<GoogleRoadsProvider> logger)
    {
        _http = http;
        _options = options.Value.Google;
        _logger = logger;
    }

    public async Task<SpeedLimitResult> GetSpeedLimitAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Google Roads provider is not configured.");
            return SpeedLimitResult.Unavailable("Google Roads API key is not configured.", ProviderKey);
        }
        
        try 
        {
            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/speedLimits?path={latitude},{longitude}&units=KPH&key={Uri.EscapeDataString(_options.ApiKey!)}";
            
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) 
            {
                var message = response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => "Google Roads rate limit reached.",
                    HttpStatusCode.Forbidden => "Google Roads request was rejected. Check the API key and quota configuration.",
                    _ => $"Google Roads request failed with HTTP {(int)response.StatusCode}."
                };

                _logger.LogWarning("Google Roads lookup failed with status code {StatusCode}.", response.StatusCode);
                return SpeedLimitResult.ProviderFailure(ProviderKey, message);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("speedLimits", out var limits) && limits.GetArrayLength() > 0)
            {
                var firstLimit = limits[0];
                double speedLimit = firstLimit.GetProperty("speedLimit").GetDouble();
                string? placeId = firstLimit.TryGetProperty("placeId", out var placeIdElement)
                    ? placeIdElement.GetString()
                    : null;
                string? warningMessage = root.TryGetProperty("warning_message", out var warningElement)
                    ? warningElement.GetString()
                    : null;

                return new SpeedLimitResult 
                { 
                    SpeedLimitKph = speedLimit,
                    SegmentIdentifier = placeId,
                    Source = ProviderKey,
                    Status = SpeedLimitLookupStatuses.Success,
                    Confidence = 0.92,
                    Message = warningMessage
                };
            }

            _logger.LogInformation("Google Roads returned no speed limit match for coordinates {Latitude}, {Longitude}.", latitude, longitude);
            return SpeedLimitResult.NotFound(ProviderKey, message: "Google Roads returned no speed limit for this coordinate.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Roads lookup failed unexpectedly.");
            return SpeedLimitResult.ProviderFailure(ProviderKey, "Google Roads lookup failed unexpectedly.");
        }
    }
}
