using Microsoft.EntityFrameworkCore;
using SpeedAlert.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedAlert.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserSettings> UserSettings { get; }
    DbSet<DrivingSession> Sessions { get; }
    DbSet<SessionPoint> SessionPoints { get; }
    DbSet<AlertEvent> AlertEvents { get; }
    DbSet<RoadLookupCache> RoadLookupCaches { get; }
    DbSet<DeviceStatus> DeviceStatuses { get; }
    DbSet<ProviderConfig> ProviderConfigs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
