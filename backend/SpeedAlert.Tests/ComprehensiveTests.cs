using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SpeedAlert.Api.Controllers;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using SpeedAlert.Domain.Entities;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SpeedAlert.Tests;

public class ComprehensiveTests
{
    private IAppDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<SpeedAlert.Infrastructure.Persistence.AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var db = new SpeedAlert.Infrastructure.Persistence.AppDbContext(options);
        return db;
    }

    private ClaimsPrincipal GetUserPrincipal(Guid userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "mock"));
    }

    [Fact]
    public async Task SpeedLimitController_ReturnsCached_IfValid()
    {
        // Arrange
        var db = GetInMemoryDb();
        var providerMock = new Mock<ISpeedLimitProvider>();
        var controller = new SpeedLimitController(providerMock.Object, db);
        
        db.RoadLookupCaches.Add(new RoadLookupCache
        {
            CacheKey = "1.123,-4.567",
            SpeedLimitKph = 50,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        // Act
        var result = await controller.Lookup(1.123, -4.567) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var val = result.Value;
        Assert.Equal(50.0, (double)val.GetType().GetProperty("speedLimitKph").GetValue(val, null));
        Assert.True((bool)val.GetType().GetProperty("isCached").GetValue(val, null));
        // Ensure provider not called
        providerMock.Verify(x => x.GetSpeedLimitAsync(It.IsAny<double>(), It.IsAny<double>()), Times.Never);
    }

    [Fact]
    public async Task TrackingController_UploadAlert_SavesToDb()
    {
        // Arrange
        var db = GetInMemoryDb();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        
        db.Sessions.Add(new DrivingSession { Id = sessionId, UserId = userId });
        await db.SaveChangesAsync();

        var controller = new TrackingController(db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = GetUserPrincipal(userId) }
        };

        var requests = new List<AlertDto>
        {
            new AlertDto { AlertType = "Overspeed", ActualSpeed = 75, SpeedLimit = 60, Timestamp = DateTime.UtcNow }
        };

        // Act
        var result = await controller.UploadAlerts(sessionId, requests);

        // Assert
        Assert.IsType<OkResult>(result);
        var session = await db.Sessions.FirstAsync();
        Assert.Equal(1, session.AlertEventCount);
        Assert.Equal(1, session.OverspeedEventCount);
        var alert = await db.AlertEvents.FirstAsync();
        Assert.Equal(75, alert.ActualSpeedKph);
    }

    [Fact]
    public async Task TrackingController_EndSession_UpdatesState()
    {
        var db = GetInMemoryDb();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        
        db.Sessions.Add(new DrivingSession { Id = sessionId, UserId = userId, Status = "Active" });
        await db.SaveChangesAsync();

        var controller = new TrackingController(db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = GetUserPrincipal(userId) }
        };

        // Act
        var result = await controller.EndSession(sessionId, new EndSessionDto { Reason = "arrived", DistanceMeters = 5000 });

        // Assert
        Assert.IsType<OkResult>(result);
        var session = await db.Sessions.FirstAsync();
        Assert.Equal("Ended", session.Status);
        Assert.Equal(5000, session.TotalDistanceMeters);
        Assert.NotNull(session.EndedAt);
    }
}
