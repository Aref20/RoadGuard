using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SpeedAlert.Api.Controllers;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using SpeedAlert.Application.Services;
using SpeedAlert.Domain.Entities;
using SpeedAlert.Infrastructure.Persistence;
using Xunit;

namespace SpeedAlert.Tests;

public class ComprehensiveTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ClaimsPrincipal CreateUserPrincipal(Guid userId, string role = "User")
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        ], "test"));
    }

    private static T GetAnonymousProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (T)property!.GetValue(instance)!;
    }

    [Fact]
    public void AuthController_Register_IsDisabled()
    {
        using var db = CreateDbContext();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "a-very-long-test-key-1234567890",
            ["Jwt:Issuer"] = "test-issuer",
            ["Jwt:Audience"] = "test-audience"
        }).Build();

        var controller = new AuthController(db, configuration);
        var result = controller.Register(new AuthDto
        {
            Email = "user@example.com",
            Password = "password123"
        }) as ObjectResult;

        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal("AUTH_SELF_REGISTRATION_DISABLED", GetAnonymousProperty<string>(result.Value!, "code"));
    }

    [Fact]
    public async Task AuthController_Login_RejectsInactiveAccounts()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Email = "disabled@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "User",
            IsActive = false,
            Settings = new UserSettings()
        });
        await db.SaveChangesAsync();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "a-very-long-test-key-1234567890",
            ["Jwt:Issuer"] = "test-issuer",
            ["Jwt:Audience"] = "test-audience"
        }).Build();

        var controller = new AuthController(db, configuration);
        var result = await controller.Login(new AuthDto
        {
            Email = "disabled@example.com",
            Password = "password123"
        }) as ObjectResult;

        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal("AUTH_ACCOUNT_DISABLED", GetAnonymousProperty<string>(result.Value!, "code"));
    }

    [Fact]
    public async Task SpeedLimitProviderOrchestrator_UsesFallbackAndProviderAwareCache()
    {
        await using var db = CreateDbContext();
        db.ProviderConfigs.AddRange(
            new ProviderConfig
            {
                ProviderKey = SpeedProviderKeys.Google,
                IsEnabled = true,
                IsSelected = true,
                PriorityOrder = 0
            },
            new ProviderConfig
            {
                ProviderKey = SpeedProviderKeys.Here,
                IsEnabled = true,
                IsSelected = false,
                PriorityOrder = 1
            });
        await db.SaveChangesAsync();

        var google = new FakeSpeedLimitProvider(
            SpeedProviderKeys.Google,
            "Google",
            (_, _) => SpeedLimitResult.ProviderFailure(SpeedProviderKeys.Google, "google failed"));
        var here = new FakeSpeedLimitProvider(
            SpeedProviderKeys.Here,
            "HERE",
            (_, _) => new SpeedLimitResult
            {
                SpeedLimitKph = 80,
                Confidence = 0.95,
                Source = SpeedProviderKeys.Here,
                Status = SpeedLimitLookupStatuses.Success,
                RoadName = "Test Road"
            });

        var orchestrator = new SpeedLimitProviderOrchestrator(
            [google, here],
            db,
            NullLogger<SpeedLimitProviderOrchestrator>.Instance);

        var firstResult = await orchestrator.GetSpeedLimitAsync(31.95, 35.91);

        Assert.Equal(80, firstResult.SpeedLimitKph);
        Assert.Equal(SpeedProviderKeys.Here, firstResult.ProviderUsed);
        Assert.True(firstResult.FallbackUsed);
        Assert.Equal(1, google.CallCount);
        Assert.Equal(1, here.CallCount);

        var googleConfig = await db.ProviderConfigs.FirstAsync(item => item.ProviderKey == SpeedProviderKeys.Google);
        var hereConfig = await db.ProviderConfigs.FirstAsync(item => item.ProviderKey == SpeedProviderKeys.Here);
        googleConfig.IsSelected = false;
        hereConfig.IsSelected = true;
        googleConfig.PriorityOrder = 1;
        hereConfig.PriorityOrder = 0;
        await db.SaveChangesAsync();

        var secondResult = await orchestrator.GetSpeedLimitAsync(31.95, 35.91);

        Assert.Equal(80, secondResult.SpeedLimitKph);
        Assert.False(secondResult.IsCached);
        Assert.Equal(2, here.CallCount);
    }

    [Fact]
    public async Task TrackingController_StartSession_ReusesExistingActiveSession()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = userId,
            Email = "driver@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "User",
            IsActive = true,
            Settings = new UserSettings { UserId = userId }
        });
        db.Sessions.Add(new DrivingSession
        {
            Id = sessionId,
            UserId = userId,
            Status = "Active",
            SessionStartReason = "auto_detect"
        });
        await db.SaveChangesAsync();

        var controller = new TrackingController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId) }
            }
        };

        var result = await controller.StartSession(new StartSessionDto
        {
            Reason = "manual",
            IsAuto = false
        }, CancellationToken.None) as OkObjectResult;

        Assert.NotNull(result);
        Assert.True(GetAnonymousProperty<bool>(result.Value!, "reusedExisting"));
        Assert.Equal(sessionId, GetAnonymousProperty<Guid>(result.Value!, "sessionId"));
    }

    [Fact]
    public async Task TrackingController_UploadAlerts_UpdatesStatistics()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = userId,
            Email = "driver@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "User",
            IsActive = true,
            Settings = new UserSettings { UserId = userId }
        });
        db.Sessions.Add(new DrivingSession
        {
            Id = sessionId,
            UserId = userId,
            Status = "Active",
            SessionStartReason = "manual"
        });
        await db.SaveChangesAsync();

        var controller = new TrackingController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId) }
            }
        };

        var result = await controller.UploadAlerts(sessionId,
        [
            new AlertDto
            {
                AlertType = "Overspeed",
                ActualSpeed = 92,
                SpeedLimit = 80,
                Timestamp = DateTime.UtcNow
            }
        ], CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var session = await db.Sessions.FirstAsync();
        Assert.Equal(1, session.AlertEventCount);
        Assert.Equal(1, session.OverspeedEventCount);
        Assert.Equal(12, session.MostSevereOverspeedKph);
    }

    [Fact]
    public async Task TrackingController_EndSession_CalculatesDistanceFromPoints()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddMinutes(-5);

        db.Users.Add(new User
        {
            Id = userId,
            Email = "driver@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "User",
            IsActive = true,
            Settings = new UserSettings { UserId = userId }
        });
        db.Sessions.Add(new DrivingSession
        {
            Id = sessionId,
            UserId = userId,
            Status = "Active",
            SessionStartReason = "manual",
            StartedAt = startedAt
        });
        db.SessionPoints.AddRange(
            new SessionPoint
            {
                DrivingSessionId = sessionId,
                Latitude = 31.9500,
                Longitude = 35.9100,
                SpeedKph = 60,
                AccuracyMeters = 10,
                Timestamp = startedAt
            },
            new SessionPoint
            {
                DrivingSessionId = sessionId,
                Latitude = 31.9510,
                Longitude = 35.9110,
                SpeedKph = 70,
                AccuracyMeters = 8,
                Timestamp = startedAt.AddMinutes(2)
            });
        await db.SaveChangesAsync();

        var controller = new TrackingController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId) }
            }
        };

        var result = await controller.EndSession(sessionId, new EndSessionDto
        {
            Reason = "arrived"
        }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var session = await db.Sessions.FirstAsync();
        Assert.Equal("Ended", session.Status);
        Assert.True(session.TotalDistanceMeters > 0);
        Assert.Equal(70, session.MaxSpeedKph);
        Assert.True(session.AverageSpeedKph >= 60);
        Assert.NotNull(session.EndedAt);
    }

    private sealed class FakeSpeedLimitProvider : ISpeedLimitProvider
    {
        private readonly Func<double, double, SpeedLimitResult> _handler;

        public FakeSpeedLimitProvider(string providerKey, string displayName, Func<double, double, SpeedLimitResult> handler)
        {
            ProviderKey = providerKey;
            DisplayName = displayName;
            _handler = handler;
        }

        public string ProviderKey { get; }
        public string DisplayName { get; }
        public bool IsConfigured => true;
        public int CallCount { get; private set; }

        public Task<SpeedLimitResult> GetSpeedLimitAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_handler(latitude, longitude));
        }
    }
}
