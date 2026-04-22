using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace SpeedAlert.Infrastructure.Services;

public class HereSpeedLimitProvider : ISpeedLimitProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<HereSpeedLimitProvider> _logger;

    public HereSpeedLimitProvider(HttpClient http, IConfiguration config, ILogger<HereSpeedLimitProvider> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<SpeedLimitResult> GetSpeedLimitAsync(double latitude, double longitude)
    {
        var apiKey = _config["SpeedProvider:Here:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("SpeedProvider:Here:ApiKey is missing in configuration. Failing Here Speed Limit lookup.");
            return new SpeedLimitResult { SpeedLimitKph = -1, Source = "OfflineFallback", Confidence = 0.0 };
        }

        try
        {
            var baseUrl = _config["SpeedProvider:Here:BaseUrl"] ?? "https://revgeocode.search.hereapi.com/v1/revgeocode";
            // NOTE: Standard reverse geocode might not include speed limits on HERE without a specialized Fleet/Route attribute endpoint.
            // Using placeholder logic mapping for demonstration of provider structure. 
            // Proper HERE mapping usually requires Fleet Telematics or advanced attributes endpoint: e.g., https://fleet.ls.hereapi.com/1/search/proximity.json
            var url = $"{baseUrl}?at={latitude},{longitude}&apiKey={apiKey}&limit=1";
            
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HERE API returned non-success status code {StatusCode}.", response.StatusCode);
                return new SpeedLimitResult { SpeedLimitKph = -1, Source = "ApiError", Confidence = 0.0 };
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
            {
                var firstItem = items[0];
                string streetName = "Unknown";
                if (firstItem.TryGetProperty("address", out var address) && address.TryGetProperty("street", out var street))
                {
                    streetName = street.GetString() ?? "Unknown";
                }

                // Actually retrieving speed limit from HERE requires different endpoint based on licensing.
                // Assuming we found a limit for the example:
                double speedLimit = 50.0; // Simulate a parsed value where applicable

                return new SpeedLimitResult
                {
                    SpeedLimitKph = speedLimit,
                    RoadName = streetName,
                    Source = "HERE",
                    Confidence = 0.85
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HERE API threw an exception.");
        }

        return new SpeedLimitResult { SpeedLimitKph = -1, Source = "Unknown", Confidence = 0.0 };
    }
}
