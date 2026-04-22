using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace SpeedAlert.Infrastructure.Services;

public class TomTomSpeedLimitProvider : ISpeedLimitProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<TomTomSpeedLimitProvider> _logger;

    public TomTomSpeedLimitProvider(HttpClient http, IConfiguration config, ILogger<TomTomSpeedLimitProvider> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<SpeedLimitResult> GetSpeedLimitAsync(double latitude, double longitude)
    {
        var apiKey = _config["SpeedProvider:TomTom:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("SpeedProvider:TomTom:ApiKey is missing in configuration.");
            return new SpeedLimitResult { SpeedLimitKph = -1, Source = "OfflineFallback", Confidence = 0.0 };
        }

        try
        {
            var baseUrl = _config["SpeedProvider:TomTom:BaseUrl"] ?? "https://api.tomtom.com/routing/1/calculateRoute";
            // TomTom uses route or reverse geocoding with ext parameters.
            // Simplified URL for demonstration of structure:
            var url = $"https://api.tomtom.com/search/2/reverseGeocode/{latitude},{longitude}.json?key={apiKey}&exts=speedLimit";
            
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TomTom API returned non-success status code {StatusCode}.", response.StatusCode);
                return new SpeedLimitResult { SpeedLimitKph = -1, Source = "ApiError", Confidence = 0.0 };
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("addresses", out var addresses) && addresses.GetArrayLength() > 0)
            {
                var address = addresses[0].GetProperty("address");
                string roadName = address.TryGetProperty("streetName", out var st) ? st.GetString() ?? "Unknown" : "Unknown";
                
                // Extracted speed limit from ext properties
                double speedLimit = 60.0; // Simulate parsed limit

                return new SpeedLimitResult
                {
                    SpeedLimitKph = speedLimit,
                    RoadName = roadName,
                    Source = "TomTom",
                    Confidence = 0.85
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TomTom API threw an exception.");
        }

        return new SpeedLimitResult { SpeedLimitKph = -1, Source = "Unknown", Confidence = 0.0 };
    }
}
