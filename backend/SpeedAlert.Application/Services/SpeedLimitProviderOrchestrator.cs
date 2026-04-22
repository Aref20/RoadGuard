using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using SpeedAlert.Domain.Entities;

namespace SpeedAlert.Application.Services;

public interface ISpeedLimitProviderOrchestrator
{
    Task<SpeedLimitResult> GetSpeedLimitAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SpeedLimitProviderStatus>> GetProviderStatusesAsync(CancellationToken cancellationToken = default);
}

public sealed class SpeedLimitProviderOrchestrator : ISpeedLimitProviderOrchestrator
{
    private const int CachePrecision = 4;
    private readonly Dictionary<string, ISpeedLimitProvider> _providers;
    private readonly IAppDbContext _db;
    private readonly ILogger<SpeedLimitProviderOrchestrator> _logger;

    public SpeedLimitProviderOrchestrator(
        IEnumerable<ISpeedLimitProvider> providers,
        IAppDbContext db,
        ILogger<SpeedLimitProviderOrchestrator> logger)
    {
        _providers = providers.ToDictionary(provider => provider.ProviderKey, StringComparer.OrdinalIgnoreCase);
        _db = db;
        _logger = logger;
    }

    public async Task<SpeedLimitResult> GetSpeedLimitAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCoordinate(latitude, longitude))
        {
            return new SpeedLimitResult
            {
                Source = "Validation",
                Status = SpeedLimitLookupStatuses.InvalidRequest,
                Confidence = 0.0,
                Message = "Coordinates are out of range."
            };
        }

        var providerConfigs = await LoadProviderConfigsAsync(cancellationToken);
        var orderedProviders = BuildExecutionPlan(providerConfigs).ToList();
        if (orderedProviders.Count == 0)
        {
            return SpeedLimitResult.Unavailable("No enabled speed limit providers are configured.");
        }

        var selectedProviderKey = orderedProviders[0].ProviderKey;
        var cacheKey = $"{Math.Round(latitude, CachePrecision)},{Math.Round(longitude, CachePrecision)}";
        var cacheEntry = await _db.RoadLookupCaches
            .FirstOrDefaultAsync(
                entry => entry.CacheKey == cacheKey &&
                         entry.SelectedProviderKey == selectedProviderKey &&
                         entry.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (cacheEntry != null)
        {
            return new SpeedLimitResult
            {
                SpeedLimitKph = cacheEntry.SpeedLimitKph,
                RoadName = cacheEntry.RoadName,
                SegmentIdentifier = cacheEntry.SegmentIdentifier,
                Source = cacheEntry.Source,
                Status = cacheEntry.LookupStatus,
                Confidence = cacheEntry.Confidence,
                ProviderUsed = cacheEntry.ProviderUsedKey,
                FallbackUsed = cacheEntry.FallbackUsed,
                Message = cacheEntry.LookupStatus == SpeedLimitLookupStatuses.Success
                    ? null
                    : "Cached speed limit result",
                IsCached = true
            };
        }

        SpeedLimitResult? lowConfidenceCandidate = null;

        foreach (var config in orderedProviders)
        {
            if (!_providers.TryGetValue(config.ProviderKey, out var provider))
            {
                MarkProviderFailure(config, "Provider implementation is not registered.");
                continue;
            }

            if (!provider.IsConfigured)
            {
                MarkProviderUnavailable(config, "Provider API key is not configured.");
                continue;
            }

            SpeedLimitResult result;
            try
            {
                result = await provider.GetSpeedLimitAsync(latitude, longitude, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception from provider {ProviderKey}.", config.ProviderKey);
                result = SpeedLimitResult.ProviderFailure(config.ProviderKey, "Provider request failed unexpectedly.");
            }

            result.ProviderUsed = provider.ProviderKey;
            result.Source = string.IsNullOrWhiteSpace(result.Source) ? provider.ProviderKey : result.Source;
            result.FallbackUsed = !string.Equals(provider.ProviderKey, selectedProviderKey, StringComparison.OrdinalIgnoreCase);

            if (result.Status == SpeedLimitLookupStatuses.Success)
            {
                MarkProviderSuccess(config);
                await CacheSuccessfulResultAsync(cacheEntry, cacheKey, selectedProviderKey, result, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                return result;
            }

            if (result.Status == SpeedLimitLookupStatuses.LowConfidence)
            {
                MarkProviderDegraded(config, result.Message ?? "Provider returned a low-confidence speed limit.");
                lowConfidenceCandidate ??= result;
                continue;
            }

            if (result.Status == SpeedLimitLookupStatuses.NotFound)
            {
                MarkProviderUnavailable(config, result.Message ?? "Provider returned no matching road segment.");
                continue;
            }

            MarkProviderFailure(config, result.Message ?? "Provider request failed.");
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (lowConfidenceCandidate != null)
        {
            return lowConfidenceCandidate;
        }

        return SpeedLimitResult.Unavailable("Speed limit unavailable from all configured providers.");
    }

    public async Task<IReadOnlyCollection<SpeedLimitProviderStatus>> GetProviderStatusesAsync(CancellationToken cancellationToken = default)
    {
        var configs = await LoadProviderConfigsAsync(cancellationToken);
        return configs
            .OrderBy(config => config.PriorityOrder)
            .Select(config =>
            {
                var isRegistered = _providers.TryGetValue(config.ProviderKey, out var provider);
                return new SpeedLimitProviderStatus
                {
                    ProviderKey = config.ProviderKey,
                    DisplayName = provider?.DisplayName ?? config.ProviderKey,
                    IsEnabled = config.IsEnabled,
                    IsSelected = config.IsSelected,
                    PriorityOrder = config.PriorityOrder,
                    IsConfigured = provider?.IsConfigured ?? false,
                    HealthStatus = config.LastHealthStatus,
                    LastFailureReason = config.LastFailureReason,
                    LastSuccessAt = config.LastSuccessAt,
                    LastFailureAt = config.LastFailureAt,
                    UpdatedAt = config.UpdatedAt
                };
            })
            .ToArray();
    }

    private async Task<List<ProviderConfig>> LoadProviderConfigsAsync(CancellationToken cancellationToken)
    {
        var configs = await _db.ProviderConfigs
            .OrderBy(config => config.PriorityOrder)
            .ToListAsync(cancellationToken);

        foreach (var providerKey in SpeedProviderKeys.All)
        {
            if (configs.All(config => !string.Equals(config.ProviderKey, providerKey, StringComparison.OrdinalIgnoreCase)))
            {
                configs.Add(new ProviderConfig
                {
                    ProviderKey = providerKey,
                    IsEnabled = true,
                    IsSelected = string.Equals(providerKey, SpeedProviderKeys.Google, StringComparison.Ordinal),
                    PriorityOrder = configs.Count,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        return configs
            .OrderBy(config => config.PriorityOrder)
            .ToList();
    }

    private IEnumerable<ProviderConfig> BuildExecutionPlan(IEnumerable<ProviderConfig> providerConfigs)
    {
        var enabled = providerConfigs
            .Where(config => config.IsEnabled)
            .OrderBy(config => config.PriorityOrder)
            .ToList();

        if (enabled.Count == 0)
        {
            return [];
        }

        var selected = enabled.FirstOrDefault(config => config.IsSelected);
        if (selected == null)
        {
            selected = enabled[0];
        }

        return new[] { selected }
            .Concat(enabled.Where(config => !string.Equals(config.ProviderKey, selected.ProviderKey, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private async Task CacheSuccessfulResultAsync(
        RoadLookupCache? cacheEntry,
        string cacheKey,
        string selectedProviderKey,
        SpeedLimitResult result,
        CancellationToken cancellationToken)
    {
        if (!ShouldCache(result))
        {
            return;
        }

        cacheEntry ??= await _db.RoadLookupCaches.FirstOrDefaultAsync(
            entry => entry.CacheKey == cacheKey && entry.SelectedProviderKey == selectedProviderKey,
            cancellationToken);

        if (cacheEntry == null)
        {
            cacheEntry = new RoadLookupCache
            {
                CacheKey = cacheKey,
                SelectedProviderKey = selectedProviderKey
            };

            _db.RoadLookupCaches.Add(cacheEntry);
        }

        cacheEntry.ProviderUsedKey = result.ProviderUsed;
        cacheEntry.RoadName = result.RoadName;
        cacheEntry.SegmentIdentifier = result.SegmentIdentifier;
        cacheEntry.Source = result.Source;
        cacheEntry.LookupStatus = result.Status;
        cacheEntry.SpeedLimitKph = result.SpeedLimitKph;
        cacheEntry.Confidence = result.Confidence;
        cacheEntry.FallbackUsed = result.FallbackUsed;
        cacheEntry.RetrievedAt = DateTime.UtcNow;
        cacheEntry.ExpiresAt = DateTime.UtcNow.AddHours(6);
    }

    private static bool ShouldCache(SpeedLimitResult result)
    {
        return result.Status == SpeedLimitLookupStatuses.Success &&
               result.SpeedLimitKph.HasValue &&
               result.Confidence >= 0.75;
    }

    private static bool IsValidCoordinate(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90 &&
               longitude is >= -180 and <= 180 &&
               !(latitude == 0 && longitude == 0);
    }

    private static void MarkProviderSuccess(ProviderConfig config)
    {
        config.LastHealthStatus = "Healthy";
        config.LastSuccessAt = DateTime.UtcNow;
        config.LastFailureReason = null;
        config.UpdatedAt = DateTime.UtcNow;
    }

    private static void MarkProviderDegraded(ProviderConfig config, string message)
    {
        config.LastHealthStatus = "Degraded";
        config.LastFailureReason = message;
        config.LastFailureAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;
    }

    private static void MarkProviderUnavailable(ProviderConfig config, string message)
    {
        config.LastHealthStatus = "Unavailable";
        config.LastFailureReason = message;
        config.LastFailureAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;
    }

    private static void MarkProviderFailure(ProviderConfig config, string message)
    {
        config.LastHealthStatus = "Failed";
        config.LastFailureReason = message;
        config.LastFailureAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;
    }
}
