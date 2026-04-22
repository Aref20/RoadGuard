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

public sealed class GoogleRoadsProvider : ISpeedLimitProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<SpeedProvidersOptions> _optionsMonitor;
    private readonly ILogger<GoogleRoadsProvider> _logger;

    public GoogleRoadsProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptionsMonitor<SpeedProvidersOptions> optionsMonitor,
        ILogger<GoogleRoadsProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public string ProviderKey => SpeedProviderKeys.Google;

    public string DisplayName => "Google Roads";

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
            return SpeedLimitResult.ProviderUnavailable(DisplayName, "Google Roads API key is not configured.");
        }

        var client = _httpClientFactory.CreateClient();
        var coordinate = string.Create(
            CultureInfo.InvariantCulture,
            $"{latitude},{longitude}");
        var url =
            $"{settings.BaseUrl?.TrimEnd('/')}/speedLimits?path={Uri.EscapeDataString(coordinate)}&units=KPH&key={Uri.EscapeDataString(settings.ApiKey)}";

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var message = $"Google Roads returned {(int)response.StatusCode}.";
                _logger.LogWarning(
                    "Google Roads speed limit lookup failed with status code {StatusCode}.",
                    response.StatusCode);
                return SpeedLimitResult.Error(DisplayName, message);
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("speedLimits", out var speedLimits) ||
                speedLimits.GetArrayLength() == 0)
            {
                return SpeedLimitResult.NotFound(DisplayName, "Google Roads did not return a posted speed limit.");
            }

            var firstLimit = speedLimits[0];
            if (!firstLimit.TryGetProperty("speedLimit", out var speedLimitProperty))
            {
                return SpeedLimitResult.NotFound(DisplayName, "Google Roads response did not contain a speed limit value.");
            }

            var speedLimitKph = speedLimitProperty.GetDouble();
            var placeId = firstLimit.TryGetProperty("placeId", out var placeIdProperty)
                ? placeIdProperty.GetString()
                : null;

            return new SpeedLimitResult
            {
                SpeedLimitKph = speedLimitKph,
                RoadName = null,
                SegmentIdentifier = placeId,
                ProviderUsed = ProviderKey,
                Source = DisplayName,
                Confidence = 0.95,
                Status = SpeedLimitLookupStatus.Success,
                Message = "Posted speed limit returned by Google Roads."
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Roads lookup threw an exception.");
            return SpeedLimitResult.Error(DisplayName, "Google Roads lookup failed unexpectedly.");
        }
    }
}
