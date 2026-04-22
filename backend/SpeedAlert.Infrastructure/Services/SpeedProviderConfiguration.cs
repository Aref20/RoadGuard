using Microsoft.Extensions.Configuration;
using SpeedAlert.Application.Models;
using SpeedAlert.Infrastructure.Options;

namespace SpeedAlert.Infrastructure.Services;

internal static class SpeedProviderConfiguration
{
    public static ProviderApiOptions Resolve(
        string providerKey,
        SpeedProvidersOptions options,
        IConfiguration configuration)
    {
        return providerKey switch
        {
            SpeedProviderKeys.Google => new ProviderApiOptions
            {
                ApiKey = options.Google.ApiKey
                    ?? configuration["SpeedProviders:Google:ApiKey"]
                    ?? configuration["SpeedProvider:ApiKey"],
                BaseUrl = options.Google.BaseUrl
                    ?? configuration["SpeedProviders:Google:BaseUrl"]
                    ?? configuration["SpeedProvider:BaseUrl"]
                    ?? "https://roads.googleapis.com/v1/"
            },
            SpeedProviderKeys.Here => new ProviderApiOptions
            {
                ApiKey = options.Here.ApiKey
                    ?? configuration["SpeedProviders:Here:ApiKey"]
                    ?? configuration["SpeedProvider:Here:ApiKey"],
                BaseUrl = options.Here.BaseUrl
                    ?? configuration["SpeedProviders:Here:BaseUrl"]
                    ?? configuration["SpeedProvider:Here:BaseUrl"]
                    ?? "https://routematching.hereapi.com/v8/match/routelinks"
            },
            SpeedProviderKeys.TomTom => new ProviderApiOptions
            {
                ApiKey = options.TomTom.ApiKey
                    ?? configuration["SpeedProviders:TomTom:ApiKey"]
                    ?? configuration["SpeedProvider:TomTom:ApiKey"],
                BaseUrl = options.TomTom.BaseUrl
                    ?? configuration["SpeedProviders:TomTom:BaseUrl"]
                    ?? configuration["SpeedProvider:TomTom:BaseUrl"]
                    ?? "https://api.tomtom.com/search/2/reverseGeocode"
            },
            _ => new ProviderApiOptions()
        };
    }
}
