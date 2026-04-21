using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace SpeedAlert.Infrastructure.Services;

public class GoogleRoadsProvider : ISpeedLimitProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    
    public GoogleRoadsProvider(IConfiguration config)
    {
        _http = new HttpClient();
        _config = config;
    }

    public async Task<SpeedLimitResult> GetSpeedLimitAsync(double latitude, double longitude)
    {
        var apiKey = _config["SpeedProvider:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            return new SpeedLimitResult { SpeedLimitKph = 50, Source = "OfflineFallback", Confidence = 0.5 };
        }
        
        try 
        {
            var baseUrl = _config["SpeedProvider:BaseUrl"] ?? "https://roads.googleapis.com/v1/";
            var url = $"{baseUrl}speedLimits?path={latitude},{longitude}&key={apiKey}";
            
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) 
            {
                return new SpeedLimitResult { SpeedLimitKph = 50, Source = "ApiErrorFallback", Confidence = 0.3 };
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("speedLimits", out var limits) && limits.GetArrayLength() > 0)
            {
                var firstLimit = limits[0];
                double speedLimit = firstLimit.GetProperty("speedLimit").GetDouble();
                string placeId = firstLimit.GetProperty("placeId").GetString() ?? "Unknown";

                return new SpeedLimitResult 
                { 
                    SpeedLimitKph = speedLimit, 
                    RoadName = placeId, 
                    Source = "GoogleRoads", 
                    Confidence = 0.95 
                };
            }
        }
        catch (Exception)
        {
            // Log in production
        }

        return new SpeedLimitResult { SpeedLimitKph = 50, Source = "Unknown", Confidence = 0.0 };
    }
}
