using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using System.Threading.Tasks;
using System.Linq;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAppDbContext _db;

    public AdminController(IAppDbContext db) => _db = db;

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .Select(u => new { u.Id, u.Email, u.IsActive, u.CreatedAt, u.Role })
            .ToListAsync();
        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserAdminDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
            return BadRequest(new { code = "AUTH_EMAIL_IN_USE", message = "Email already in use" });
            
        var user = new SpeedAlert.Domain.Entities.User
        {
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User",
            IsActive = true
        };
        
        user.Settings = new SpeedAlert.Domain.Entities.UserSettings { UserId = user.Id };
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        
        return Ok(new { user.Id, user.Email, user.IsActive, user.CreatedAt, user.Role });
    }

    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(System.Guid id, [FromBody] UpdateUserStatusDto payload)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "User not found" });

        if (user.Role == "Admin") return BadRequest(new { message = "Cannot modify admin status" });

        user.IsActive = payload.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new { message = "User status updated" });
    }

    [HttpPut("users/{id}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(System.Guid id, [FromBody] ResetUserPasswordDto payload)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "User not found" });

        if (user.Role == "Admin") return BadRequest(new { message = "Cannot reset admin password here" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(payload.Password);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password reset successfully" });
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var sessions = await _db.Sessions
            .OrderByDescending(s => s.StartedAt)
            .Take(100)
            .Select(s => new { 
                s.Id, 
                s.UserId, 
                s.StartedAt, 
                s.EndedAt, 
                s.Status,
                s.WasAutoStarted,
                s.SessionStartReason,
                s.SessionEndReason,
                s.OverspeedEventCount,
                s.AlertEventCount
            })
            .ToListAsync();
        return Ok(sessions);
    }

    [HttpGet("health-overview")]
    public async Task<IActionResult> GetSystemHealth()
    {
        var totalUsers = await _db.Users.CountAsync();
        var totalSessions = await _db.Sessions.CountAsync();
        var activeSessions = await _db.Sessions.CountAsync(s => s.Status == "Active");
        var autoStartedSessions = await _db.Sessions.CountAsync(s => s.WasAutoStarted);
        
        var totalViolations = await _db.Sessions.SumAsync(s => s.OverspeedEventCount);
        var totalAlerts = await _db.Sessions.SumAsync(s => s.AlertEventCount);

        bool dbHealthy = false;
        try {
            dbHealthy = await ((Microsoft.EntityFrameworkCore.DbContext)_db).Database.CanConnectAsync();
        } catch { }

        return Ok(new
        {
            TotalUsers = totalUsers,
            TotalSessions = totalSessions,
            ActiveSessions = activeSessions,
            AutoStartedSessions = autoStartedSessions,
            TotalViolations = totalViolations,
            TotalAlerts = totalAlerts,
            ServerTime = System.DateTime.UtcNow,
            ApiVersion = "1.0",
            DatabaseStatus = dbHealthy ? "Healthy" : "Disconnected"
        });
    }

    [HttpGet("provider-settings")]
    public async Task<IActionResult> GetProviderSettings()
    {
        var configs = await _db.ProviderConfigs.OrderBy(p => p.PriorityOrder).ToListAsync();
        
        // Seed default providers if missed by migration
        var defaultProviders = new[] { "GoogleRoads", "Here", "TomTom" };
        bool addedNew = false;
        foreach (var pKey in defaultProviders)
        {
            if (!configs.Any(c => c.ProviderKey == pKey))
            {
                var newConfig = new SpeedAlert.Domain.Entities.ProviderConfig
                {
                    ProviderKey = pKey,
                    IsEnabled = true,
                    IsSelected = pKey == "GoogleRoads",
                    PriorityOrder = pKey == "GoogleRoads" ? 0 : 1,
                    UpdatedAt = System.DateTime.UtcNow
                };
                _db.ProviderConfigs.Add(newConfig);
                configs.Add(newConfig);
                addedNew = true;
            }
        }
        
        if (addedNew)
        {
            await _db.SaveChangesAsync();
        }

        return Ok(configs.Select(c => new
        {
            c.ProviderKey,
            DisplayName = c.ProviderKey,
            c.IsEnabled,
            c.IsSelected,
            c.PriorityOrder,
            c.UpdatedAt
        }));
    }

    [HttpPut("provider-settings")]
    public async Task<IActionResult> UpdateProviderSettings([FromBody] System.Collections.Generic.List<ProviderConfigUpdateDto> payload)
    {
        var configs = await _db.ProviderConfigs.ToListAsync();
        
        // Ensure only one is selected
        var selectedCount = payload.Count(p => p.IsSelected);
        if (selectedCount > 1) return BadRequest("Only one provider can be selected");

        foreach (var update in payload)
        {
            var conf = configs.FirstOrDefault(c => c.ProviderKey == update.ProviderKey);
            if (conf != null)
            {
                conf.IsEnabled = update.IsEnabled;
                conf.IsSelected = update.IsSelected;
                conf.PriorityOrder = update.PriorityOrder;
                conf.UpdatedAt = System.DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Provider settings updated successfully" });
    }
}

public class ProviderConfigUpdateDto
{
    public string ProviderKey { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public bool IsSelected { get; set; }
    public int PriorityOrder { get; set; }
}

public class CreateUserAdminDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = null!;
}

public class UpdateUserStatusDto
{
    public bool IsActive { get; set; }
}

public class ResetUserPasswordDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = null!;
}
