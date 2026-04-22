using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Models;
using SpeedAlert.Application.Services;
using SpeedAlert.Domain.Entities;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ISpeedLimitProviderOrchestrator _orchestrator;

    public AdminController(IAppDbContext db, ISpeedLimitProviderOrchestrator orchestrator)
    {
        _db = db;
        _orchestrator = orchestrator;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .OrderByDescending(user => user.CreatedAt)
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.IsActive,
                user.CreatedAt,
                user.Role
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserAdminDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
        {
            return Conflict(new { code = "AUTH_EMAIL_IN_USE", message = "Email already in use." });
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User",
            IsActive = true
        };

        user.Settings = new UserSettings { UserId = user.Id };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            user.Id,
            user.Email,
            user.IsActive,
            user.CreatedAt,
            user.Role
        });
    }

    [HttpPut("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusDto payload, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (user.Role == "Admin")
        {
            return BadRequest(new { code = "ADMIN_STATUS_LOCKED", message = "Admin accounts cannot be activated or deactivated from this screen." });
        }

        user.IsActive = payload.IsActive;

        if (!payload.IsActive)
        {
            var activeSessions = await _db.Sessions
                .Where(session => session.UserId == id && session.Status == "Active")
                .ToListAsync(cancellationToken);

            foreach (var session in activeSessions)
            {
                session.Status = "Ended";
                session.EndedAt = DateTime.UtcNow;
                session.SessionEndReason = "account_deactivated";
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "User status updated successfully." });
    }

    [HttpPut("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(Guid id, [FromBody] ResetUserPasswordDto payload, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (user.Role == "Admin")
        {
            return BadRequest(new { code = "ADMIN_PASSWORD_RESET_LOCKED", message = "Admin passwords cannot be reset from this screen." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(payload.Password);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Password reset successfully." });
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var sessions = await _db.Sessions
            .OrderByDescending(session => session.StartedAt)
            .Take(100)
            .Select(session => new
            {
                session.Id,
                session.UserId,
                session.StartedAt,
                session.EndedAt,
                session.Status,
                session.WasAutoStarted,
                session.SessionStartReason,
                session.SessionEndReason,
                session.OverspeedEventCount,
                session.AlertEventCount,
                session.TotalDistanceMeters,
                session.AverageSpeedKph,
                session.MaxSpeedKph
            })
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }

    [HttpGet("health-overview")]
    public async Task<IActionResult> GetSystemHealth(CancellationToken cancellationToken)
    {
        var totalUsers = await _db.Users.CountAsync(cancellationToken);
        var totalSessions = await _db.Sessions.CountAsync(cancellationToken);
        var activeSessions = await _db.Sessions.CountAsync(session => session.Status == "Active", cancellationToken);
        var autoStartedSessions = await _db.Sessions.CountAsync(session => session.WasAutoStarted, cancellationToken);
        var totalViolations = await _db.Sessions.SumAsync(session => session.OverspeedEventCount, cancellationToken);
        var totalAlerts = await _db.Sessions.SumAsync(session => session.AlertEventCount, cancellationToken);
        var providerStatuses = await _orchestrator.GetProviderStatusesAsync(cancellationToken);
        var selectedProvider = providerStatuses.FirstOrDefault(status => status.IsSelected);

        var databaseStatus = "Disconnected";
        try
        {
            databaseStatus = await ((DbContext)_db).Database.CanConnectAsync(cancellationToken)
                ? "Healthy"
                : "Disconnected";
        }
        catch
        {
            databaseStatus = "Disconnected";
        }

        return Ok(new
        {
            TotalUsers = totalUsers,
            TotalSessions = totalSessions,
            ActiveSessions = activeSessions,
            AutoStartedSessions = autoStartedSessions,
            TotalViolations = totalViolations,
            TotalAlerts = totalAlerts,
            ServerTime = DateTime.UtcNow,
            ApiVersion = "1.0",
            DatabaseStatus = databaseStatus,
            SelectedProvider = selectedProvider?.ProviderKey,
            ProviderHealth = selectedProvider?.HealthStatus ?? "Unknown"
        });
    }

    [HttpGet("provider-settings")]
    public async Task<IActionResult> GetProviderSettings(CancellationToken cancellationToken)
    {
        var statuses = await _orchestrator.GetProviderStatusesAsync(cancellationToken);
        return Ok(statuses);
    }

    [HttpGet("provider-health")]
    public async Task<IActionResult> GetProviderHealth(CancellationToken cancellationToken)
    {
        var statuses = await _orchestrator.GetProviderStatusesAsync(cancellationToken);
        return Ok(statuses);
    }

    [HttpPut("provider-settings")]
    public async Task<IActionResult> UpdateProviderSettings([FromBody] List<ProviderConfigUpdateDto> payload, CancellationToken cancellationToken)
    {
        if (payload.Count == 0)
        {
            return BadRequest(new { code = "PROVIDER_SETTINGS_EMPTY", message = "At least one provider configuration is required." });
        }

        var selectedProviders = payload.Where(item => item.IsSelected).ToList();
        if (selectedProviders.Count != 1)
        {
            return BadRequest(new { code = "PROVIDER_SELECTION_INVALID", message = "Exactly one provider must be selected." });
        }

        if (!selectedProviders[0].IsEnabled)
        {
            return BadRequest(new { code = "PROVIDER_SELECTION_DISABLED", message = "The selected provider must be enabled." });
        }

        var duplicatePriorities = payload
            .GroupBy(item => item.PriorityOrder)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicatePriorities.Length > 0)
        {
            return BadRequest(new { code = "PROVIDER_PRIORITY_DUPLICATE", message = "Each provider must have a unique priority order." });
        }

        var existingConfigs = await _db.ProviderConfigs.ToListAsync(cancellationToken);
        foreach (var update in payload)
        {
            var config = existingConfigs.FirstOrDefault(item => item.ProviderKey == update.ProviderKey);
            if (config == null)
            {
                return BadRequest(new { code = "PROVIDER_UNKNOWN", message = $"Unknown provider '{update.ProviderKey}'." });
            }

            config.IsEnabled = update.IsEnabled;
            config.IsSelected = update.IsSelected;
            config.PriorityOrder = update.PriorityOrder;
            config.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Provider settings updated successfully." });
    }
}

public sealed class ProviderConfigUpdateDto
{
    [Required]
    public string ProviderKey { get; set; } = null!;

    public bool IsEnabled { get; set; }
    public bool IsSelected { get; set; }
    public int PriorityOrder { get; set; }
}

public sealed class CreateUserAdminDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = null!;
}

public sealed class UpdateUserStatusDto
{
    public bool IsActive { get; set; }
}

public sealed class ResetUserPasswordDto
{
    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = null!;
}
