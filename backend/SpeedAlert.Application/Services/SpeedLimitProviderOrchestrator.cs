using System;
using System.Collections.Generic;
using System.Globalization;
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
    Task<SpeedLimitResult> LookupAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);

    Task<SpeedProviderSettingsResponse> GetProviderSettingsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpeedProviderStatusDto>> GetProviderHealthAsync(
        CancellationToken cancellationToken = default);

    Task UpdateProviderSettingsAsync(
        UpdateProviderSettingsRequest request,
        string? updatedBy,
        CancellationToken cancellationToken = default);

    Task<SpeedLimitResult> TestProviderAsync(
        string providerKey,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}

public sealed class SpeedLimitProviderOrchestrator : ISpeedLimitProviderOrchestrator
{
    private const string LegacyGoogleRoadsKey = "GoogleRoads";

    private readonly Dictionary<string, ISpeedLimitProvider> _providers;
    private readonly IAppDbContext _db;
    private readonly ILogger<SpeedLimitProviderOrchestrator> _logger;

    public SpeedLimitProviderOrchestrator(
        IEnumerable<ISpeedLimitProvider> providers,
        IAppDbContext db,
        ILogger<SpeedLimitProviderOrchestrator> logger)
    {
        _providers = providers.ToDictionary(p => p.ProviderKey, StringComparer.OrdinalIgnoreCase);
        _db = db;
        _logger = logger;
    }

    public async Task<SpeedLimitResult> LookupAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var configs = await EnsureProviderConfigurationsAsync(cancellationToken);
        var runtimeSettings = await EnsureProviderRuntimeSettingsAsync(cancellationToken);
        var strategyKey = BuildStrategyKey(configs, runtimeSettings.FallbackEnabled);
        var cacheKey = BuildCacheKey(latitude, longitude, strategyKey);

        var cacheEntry = await _db.RoadLookupCaches
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CacheKey == cacheKey, cancellationToken);

        if (cacheEntry is not null && cacheEntry.ExpiresAt > DateTime.UtcNow)
        {
            return new SpeedLimitResult
            {
                SpeedLimitKph = cacheEntry.SpeedLimitKph,
                RoadName = cacheEntry.RoadName,
                SegmentIdentifier = cacheEntry.SegmentIdentifier,
                Source = cacheEntry.Source,
                ProviderUsed = cacheEntry.ProviderKey,
                Confidence = cacheEntry.Confidence,
                Status = SpeedLimitLookupStatus.Success,
                IsCached = true,
                Message = "Speed limit served from lookup cache."
            };
        }

        var lookupPlan = BuildLookupPlan(configs, runtimeSettings.FallbackEnabled);
        if (lookupPlan.Count == 0)
        {
            return SpeedLimitResult.NotFound("Unavailable", "No enabled speed limit providers are configured.");
        }

        var fallbackUsed = false;

        foreach (var config in lookupPlan)
        {
            var normalizedKey = NormalizeProviderKey(config.ProviderKey);
            if (!_providers.TryGetValue(normalizedKey, out var provider))
            {
                MarkProviderFailure(config, "Provider is not registered in the API container.");
                continue;
            }

            if (!provider.IsConfigured)
            {
                MarkProviderUnavailable(config, $"{provider.DisplayName} credentials are not configured.");
                continue;
            }

            var result = await provider.GetSpeedLimitAsync(latitude, longitude, cancellationToken);
            result.ProviderUsed ??= provider.ProviderKey;
            result.Source = string.IsNullOrWhiteSpace(result.Source) ? provider.DisplayName : result.Source;
            result.FallbackUsed = fallbackUsed;

            if (result.IsTrustworthy)
            {
                MarkProviderSuccess(config);
                await UpsertCacheAsync(cacheKey, strategyKey, result, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                return result;
            }

            MarkProviderFailure(config, result.Message ?? $"Lookup ended with status {result.Status}.");
            fallbackUsed = true;
            _logger.LogWarning(
                "Speed limit lookup failed for provider {ProviderKey} with status {Status}.",
                provider.ProviderKey,
                result.Status);

            if (!runtimeSettings.FallbackEnabled)
            {
                await _db.SaveChangesAsync(cancellationToken);
                return FinalizeFailureResult(result, provider.ProviderKey);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new SpeedLimitResult
        {
            Source = "Unavailable",
            Status = SpeedLimitLookupStatus.NotFound,
            Confidence = 0,
            Message = "Speed limit unavailable from all configured providers.",
            FallbackUsed = lookupPlan.Count > 1
        };
    }

    public async Task<SpeedProviderSettingsResponse> GetProviderSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var configs = await EnsureProviderConfigurationsAsync(cancellationToken);
        var runtimeSettings = await EnsureProviderRuntimeSettingsAsync(cancellationToken);

        return new SpeedProviderSettingsResponse
        {
            FallbackEnabled = runtimeSettings.FallbackEnabled,
            Providers = configs
                .OrderBy(c => c.PriorityOrder)
                .Select(MapProviderStatus)
                .ToList()
        };
    }

    public async Task<IReadOnlyList<SpeedProviderStatusDto>> GetProviderHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await GetProviderSettingsAsync(cancellationToken);
        return settings.Providers;
    }

    public async Task UpdateProviderSettingsAsync(
        UpdateProviderSettingsRequest request,
        string? updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (request.Providers.Count == 0)
        {
            throw new InvalidOperationException("At least one provider configuration is required.");
        }

        var selectedProviders = request.Providers.Count(p => p.IsSelected);
        if (selectedProviders != 1)
        {
            throw new InvalidOperationException("Exactly one provider must be selected.");
        }

        if (request.Providers.Any(p => p.IsSelected && !p.IsEnabled))
        {
            throw new InvalidOperationException("The selected provider must be enabled.");
        }

        var configs = await EnsureProviderConfigurationsAsync(cancellationToken);
        var runtimeSettings = await EnsureProviderRuntimeSettingsAsync(cancellationToken);

        foreach (var update in request.Providers)
        {
            var normalizedKey = NormalizeProviderKey(update.ProviderKey);
            var config = configs.FirstOrDefault(c => NormalizeProviderKey(c.ProviderKey) == normalizedKey);
            if (config is null)
            {
                continue;
            }

            config.IsEnabled = update.IsEnabled;
            config.IsSelected = update.IsSelected;
            config.PriorityOrder = update.PriorityOrder;
            config.UpdatedAt = DateTime.UtcNow;
            config.UpdatedBy = updatedBy;
        }

        runtimeSettings.FallbackEnabled = request.FallbackEnabled;
        runtimeSettings.UpdatedAt = DateTime.UtcNow;
        runtimeSettings.UpdatedBy = updatedBy;

        NormalizeSelections(configs);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SpeedLimitResult> TestProviderAsync(
        string providerKey,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var configs = await EnsureProviderConfigurationsAsync(cancellationToken);
        var normalizedKey = NormalizeProviderKey(providerKey);
        var config = configs.FirstOrDefault(c => NormalizeProviderKey(c.ProviderKey) == normalizedKey);

        if (config is null)
        {
            return SpeedLimitResult.ProviderUnavailable("Unavailable", $"Provider '{providerKey}' is unknown.");
        }

        if (!_providers.TryGetValue(normalizedKey, out var provider))
        {
            return SpeedLimitResult.ProviderUnavailable("Unavailable", $"Provider '{providerKey}' is not registered.");
        }

        if (!provider.IsConfigured)
        {
            MarkProviderUnavailable(config, $"{provider.DisplayName} credentials are not configured.");
            await _db.SaveChangesAsync(cancellationToken);
            return SpeedLimitResult.ProviderUnavailable(provider.DisplayName, $"{provider.DisplayName} credentials are not configured.");
        }

        var result = await provider.GetSpeedLimitAsync(latitude, longitude, cancellationToken);
        result.ProviderUsed ??= provider.ProviderKey;
        result.Source = string.IsNullOrWhiteSpace(result.Source) ? provider.DisplayName : result.Source;

        if (result.IsTrustworthy)
        {
            MarkProviderSuccess(config);
        }
        else
        {
            MarkProviderFailure(config, result.Message ?? $"Lookup ended with status {result.Status}.");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<List<ProviderConfig>> EnsureProviderConfigurationsAsync(CancellationToken cancellationToken)
    {
        var configs = await _db.ProviderConfigs
            .OrderBy(c => c.PriorityOrder)
            .ToListAsync(cancellationToken);

        var hasChanges = false;

        var legacyGoogle = configs.FirstOrDefault(c => c.ProviderKey == LegacyGoogleRoadsKey);
        if (legacyGoogle is not null && configs.All(c => c.ProviderKey != SpeedProviderKeys.Google))
        {
            _db.ProviderConfigs.Add(new ProviderConfig
            {
                ProviderKey = SpeedProviderKeys.Google,
                IsEnabled = legacyGoogle.IsEnabled,
                IsSelected = legacyGoogle.IsSelected,
                PriorityOrder = legacyGoogle.PriorityOrder,
                UpdatedAt = legacyGoogle.UpdatedAt,
                UpdatedBy = legacyGoogle.UpdatedBy,
                LastSuccessfulLookupAt = legacyGoogle.LastSuccessfulLookupAt,
                LastFailureAt = legacyGoogle.LastFailureAt,
                LastFailureReason = legacyGoogle.LastFailureReason,
                LastHealthStatus = legacyGoogle.LastHealthStatus
            });
            _db.ProviderConfigs.Remove(legacyGoogle);
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _db.SaveChangesAsync(cancellationToken);
            configs = await _db.ProviderConfigs.OrderBy(c => c.PriorityOrder).ToListAsync(cancellationToken);
        }

        foreach (var providerKey in SpeedProviderKeys.All)
        {
            if (configs.Any(c => NormalizeProviderKey(c.ProviderKey) == providerKey))
            {
                continue;
            }

            configs.Add(new ProviderConfig
            {
                ProviderKey = providerKey,
                IsEnabled = true,
                IsSelected = providerKey == SpeedProviderKeys.Google,
                PriorityOrder = providerKey switch
                {
                    SpeedProviderKeys.Google => 0,
                    SpeedProviderKeys.Here => 1,
                    _ => 2
                },
                UpdatedAt = DateTime.UtcNow,
                LastHealthStatus = SpeedProviderHealthStatuses.Unknown
            });
            hasChanges = true;
        }

        if (NormalizeSelections(configs))
        {
            hasChanges = true;
        }

        if (hasChanges)
        {
            if (_db.ProviderConfigs is DbSet<ProviderConfig> providerConfigs)
            {
                foreach (var config in configs.Where(c => providerConfigs.Local.All(local => local.ProviderKey != c.ProviderKey)))
                {
                    providerConfigs.Add(config);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            configs = await _db.ProviderConfigs.OrderBy(c => c.PriorityOrder).ToListAsync(cancellationToken);
        }

        return configs;
    }

    private async Task<ProviderRuntimeSettings> EnsureProviderRuntimeSettingsAsync(CancellationToken cancellationToken)
    {
        var runtimeSettings = await _db.ProviderRuntimeSettings.FirstOrDefaultAsync(cancellationToken);
        if (runtimeSettings is not null)
        {
            return runtimeSettings;
        }

        runtimeSettings = new ProviderRuntimeSettings
        {
            Id = 1,
            FallbackEnabled = true,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ProviderRuntimeSettings.Add(runtimeSettings);
        await _db.SaveChangesAsync(cancellationToken);
        return runtimeSettings;
    }

    private IReadOnlyList<ProviderConfig> BuildLookupPlan(
        IEnumerable<ProviderConfig> configs,
        bool fallbackEnabled)
    {
        var enabledConfigs = configs
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.PriorityOrder)
            .ToList();

        if (enabledConfigs.Count == 0)
        {
            return [];
        }

        var selected = enabledConfigs.FirstOrDefault(c => c.IsSelected) ?? enabledConfigs.First();
        var plan = new List<ProviderConfig> { selected };

        if (fallbackEnabled)
        {
            plan.AddRange(enabledConfigs.Where(c => c.ProviderKey != selected.ProviderKey));
        }

        return plan;
    }

    private SpeedProviderStatusDto MapProviderStatus(ProviderConfig config)
    {
        var normalizedKey = NormalizeProviderKey(config.ProviderKey);
        var provider = _providers.TryGetValue(normalizedKey, out var registeredProvider) ? registeredProvider : null;
        var isConfigured = provider?.IsConfigured ?? false;

        return new SpeedProviderStatusDto
        {
            ProviderKey = normalizedKey,
            DisplayName = provider?.DisplayName ?? normalizedKey,
            IsEnabled = config.IsEnabled,
            IsSelected = config.IsSelected,
            PriorityOrder = config.PriorityOrder,
            IsConfigured = isConfigured,
            HealthStatus = DetermineHealthStatus(config, isConfigured),
            UpdatedAt = config.UpdatedAt,
            LastSuccessfulLookupAt = config.LastSuccessfulLookupAt,
            LastFailureAt = config.LastFailureAt,
            LastFailureReason = config.LastFailureReason
        };
    }

    private static string BuildCacheKey(double latitude, double longitude, string strategyKey)
    {
        var roundedLatitude = Math.Round(latitude, 4).ToString("F4", CultureInfo.InvariantCulture);
        var roundedLongitude = Math.Round(longitude, 4).ToString("F4", CultureInfo.InvariantCulture);
        return $"{roundedLatitude},{roundedLongitude}|{strategyKey}";
    }

    private static string BuildStrategyKey(IEnumerable<ProviderConfig> configs, bool fallbackEnabled)
    {
        var orderedConfigs = configs
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.IsSelected ? 0 : 1)
            .ThenBy(c => c.PriorityOrder)
            .Select(c => $"{NormalizeProviderKey(c.ProviderKey)}:{c.PriorityOrder}:{c.IsSelected}");

        return $"{fallbackEnabled}|{string.Join(",", orderedConfigs)}";
    }

    private async Task UpsertCacheAsync(
        string cacheKey,
        string strategyKey,
        SpeedLimitResult result,
        CancellationToken cancellationToken)
    {
        if (!result.IsTrustworthy || result.SpeedLimitKph is null)
        {
            return;
        }

        var existingCache = await _db.RoadLookupCaches
            .FirstOrDefaultAsync(c => c.CacheKey == cacheKey, cancellationToken);

        if (existingCache is null)
        {
            _db.RoadLookupCaches.Add(new RoadLookupCache
            {
                CacheKey = cacheKey,
                StrategyKey = strategyKey,
                RoadName = result.RoadName ?? "Unknown",
                SpeedLimitKph = result.SpeedLimitKph.Value,
                ProviderKey = result.ProviderUsed ?? result.Source,
                Source = result.Source,
                SegmentIdentifier = result.SegmentIdentifier,
                Confidence = result.Confidence,
                RetrievedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(12)
            });

            return;
        }

        existingCache.StrategyKey = strategyKey;
        existingCache.RoadName = result.RoadName ?? existingCache.RoadName;
        existingCache.SpeedLimitKph = result.SpeedLimitKph.Value;
        existingCache.ProviderKey = result.ProviderUsed ?? result.Source;
        existingCache.Source = result.Source;
        existingCache.SegmentIdentifier = result.SegmentIdentifier;
        existingCache.Confidence = result.Confidence;
        existingCache.RetrievedAt = DateTime.UtcNow;
        existingCache.ExpiresAt = DateTime.UtcNow.AddHours(12);
    }

    private static string NormalizeProviderKey(string providerKey)
    {
        return providerKey.Equals(LegacyGoogleRoadsKey, StringComparison.OrdinalIgnoreCase)
            ? SpeedProviderKeys.Google
            : providerKey;
    }

    private static bool NormalizeSelections(ICollection<ProviderConfig> configs)
    {
        var hasChanges = false;
        var orderedConfigs = configs.OrderBy(c => c.PriorityOrder).ToList();

        var selected = orderedConfigs.FirstOrDefault(c => c.IsSelected && c.IsEnabled)
            ?? orderedConfigs.FirstOrDefault(c => NormalizeProviderKey(c.ProviderKey) == SpeedProviderKeys.Google && c.IsEnabled)
            ?? orderedConfigs.FirstOrDefault(c => c.IsEnabled);

        foreach (var config in orderedConfigs)
        {
            var shouldBeSelected = selected is not null && config.ProviderKey == selected.ProviderKey;
            if (config.IsSelected != shouldBeSelected)
            {
                config.IsSelected = shouldBeSelected;
                hasChanges = true;
            }
        }

        var priority = 0;
        foreach (var config in orderedConfigs.OrderBy(c => c.PriorityOrder))
        {
            if (config.PriorityOrder != priority)
            {
                config.PriorityOrder = priority;
                hasChanges = true;
            }

            priority++;
        }

        return hasChanges;
    }

    private void MarkProviderSuccess(ProviderConfig config)
    {
        config.LastSuccessfulLookupAt = DateTime.UtcNow;
        config.LastHealthStatus = SpeedProviderHealthStatuses.Healthy;
        config.LastFailureReason = null;
    }

    private void MarkProviderFailure(ProviderConfig config, string reason)
    {
        config.LastFailureAt = DateTime.UtcNow;
        config.LastFailureReason = reason;
        config.LastHealthStatus = SpeedProviderHealthStatuses.Degraded;
    }

    private void MarkProviderUnavailable(ProviderConfig config, string reason)
    {
        config.LastFailureAt = DateTime.UtcNow;
        config.LastFailureReason = reason;
        config.LastHealthStatus = SpeedProviderHealthStatuses.NotConfigured;
    }

    private static string DetermineHealthStatus(ProviderConfig config, bool isConfigured)
    {
        if (!config.IsEnabled)
        {
            return SpeedProviderHealthStatuses.Disabled;
        }

        if (!isConfigured)
        {
            return SpeedProviderHealthStatuses.NotConfigured;
        }

        return string.IsNullOrWhiteSpace(config.LastHealthStatus)
            ? SpeedProviderHealthStatuses.Unknown
            : config.LastHealthStatus;
    }

    private static SpeedLimitResult FinalizeFailureResult(SpeedLimitResult result, string providerKey)
    {
        result.ProviderUsed ??= providerKey;
        result.Source = string.IsNullOrWhiteSpace(result.Source) ? providerKey : result.Source;
        return result;
    }
}
