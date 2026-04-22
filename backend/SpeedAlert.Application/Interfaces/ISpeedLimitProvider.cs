using System.Threading;
using System.Threading.Tasks;
using SpeedAlert.Application.Models;

namespace SpeedAlert.Application.Interfaces;

public interface ISpeedLimitProvider
{
    string ProviderKey { get; }

    string DisplayName { get; }

    bool IsConfigured { get; }

    Task<SpeedLimitResult> GetSpeedLimitAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}
