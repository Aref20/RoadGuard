using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SpeedAlert.Application.Services;

public interface ISpeedLimitProviderOrchestrator
{
    Task<SpeedLimitResult> GetSpeedLimitAsync(double latitude, double longitude);
}

public class SpeedLimitProviderOrchestrator : ISpeedLimitProviderOrchestrator
{
    private readonly IEnumerable<ISpeedLimitProvider> _providers;
    private readonly IAppDbContext _db;
    private readonly ILogger<SpeedLimitProviderOrchestrator> _logger;

    public SpeedLimitProviderOrchestrator(
        IEnumerable<ISpeedLimitProvider> providers,
        IAppDbContext db,
        ILogger<SpeedLimitProviderOrchestrator> logger)
    {
        _providers = providers;
        _db = db;
        _logger = logger;
    }

    public async Task<SpeedLimitResult> GetSpeedLimitAsync(double latitude, double longitude)
    {
        var configs = await _db.ProviderConfigs.ToListAsync();

        // 1. If configured, try the selected provider first.
        var selectedConfig = configs.FirstOrDefault(c => c.IsSelected && c.IsEnabled);
        if (selectedConfig != null)
        {
            var result = await TryProviderAsync(selectedConfig.ProviderKey, latitude, longitude);
            if (IsTrustworthy(result))
            {
                result.ProviderUsed = selectedConfig.ProviderKey;
                return result;
            }
            _logger.LogWarning("Selected provider {ProviderKey} failed to return a trustworthy result. Falling back.", selectedConfig.ProviderKey);
        }

        // 2. Fallback prioritizing the enabled ones sorted by priority order
        var fallbackConfigs = configs.Where(c => c.IsEnabled && (!c.IsSelected || selectedConfig == null))
                                     .OrderBy(c => c.PriorityOrder)
                                     .ToList();

        foreach (var config in fallbackConfigs)
        {
            var result = await TryProviderAsync(config.ProviderKey, latitude, longitude);
            if (IsTrustworthy(result))
            {
                result.ProviderUsed = config.ProviderKey;
                result.FallbackUsed = true;
                return result;
            }
            _logger.LogWarning("Fallback provider {ProviderKey} failed.", config.ProviderKey);
        }

        // 3. Last resort if no DB config is present or all failed, just try any provider we have.
        foreach (var provider in _providers)
        {
            var key = provider.GetType().Name.Replace("SpeedLimitProvider", "").Replace("Provider", ""); // Example: GoogleRoadsProvider -> GoogleRoads
            if (configs.Any(c => c.ProviderKey.Equals(key, StringComparison.OrdinalIgnoreCase) && !c.IsEnabled))
            {
                continue; // Admin explicitly disabled it
            }

            var result = await TryProviderAsync(key, latitude, longitude);
            if (IsTrustworthy(result))
            {
                result.ProviderUsed = key;
                result.FallbackUsed = true;
                return result;
            }
        }

        return new SpeedLimitResult 
        { 
            SpeedLimitKph = -1, 
            Source = "Unavailable", 
            Confidence = 0.0, 
            Message = "Speed limit unavailable from all providers" 
        };
    }

    private async Task<SpeedLimitResult> TryProviderAsync(string providerKey, double latitude, double longitude)
    {
        // Try to match the providerKey to our registered providers by convention
        var provider = _providers.FirstOrDefault(p => 
            p.GetType().Name.Contains(providerKey, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
            return new SpeedLimitResult { SpeedLimitKph = -1, Confidence = 0.0 };

        try
        {
            return await provider.GetSpeedLimitAsync(latitude, longitude);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider {ProviderName} threw an unhandled exception.", provider.GetType().Name);
            return new SpeedLimitResult { SpeedLimitKph = -1, Confidence = 0.0 };
        }
    }

    private bool IsTrustworthy(SpeedLimitResult result)
    {
        return result != null && result.SpeedLimitKph > 0 && result.Confidence > 0.0;
    }
}
