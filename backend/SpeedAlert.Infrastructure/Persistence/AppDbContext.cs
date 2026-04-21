using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Domain.Entities;

namespace SpeedAlert.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<DrivingSession> Sessions => Set<DrivingSession>();
    public DbSet<SessionPoint> SessionPoints => Set<SessionPoint>();
    public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();
    public DbSet<RoadLookupCache> RoadLookupCaches => Set<RoadLookupCache>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<UserSettings>()
            .HasKey(us => us.UserId);
        
        modelBuilder.Entity<UserSettings>()
            .HasOne(us => us.User)
            .WithOne(u => u.Settings)
            .HasForeignKey<UserSettings>(us => us.UserId);

        modelBuilder.Entity<RoadLookupCache>()
            .HasIndex(r => r.CacheKey)
            .IsUnique();
    }
}
