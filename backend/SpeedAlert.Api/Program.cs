using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Serilog;
using SpeedAlert.Infrastructure;
using System;
using Microsoft.EntityFrameworkCore;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Infrastructure.Persistence;
using SpeedAlert.Domain.Entities;
using System.Linq;
using System.Threading.Tasks;
using SpeedAlert.Api.Hubs;
using SpeedAlert.Api.Services;
using SpeedAlert.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog (Console + Rolling File)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/speedalert-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddHostedService<TelemetryBroadcastService>();

// CORS Config
var configuredAllowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var allowedOrigins = configuredAllowedOrigins
    .SelectMany(origin => origin.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .Where(origin => !string.IsNullOrWhiteSpace(origin) && origin != "*")
    .Concat(new[]
    {
        "http://localhost:3000",
        "http://localhost:3001",
        "https://roadguard-production.up.railway.app",
        "https://*.up.railway.app"
    })
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SecureCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .SetIsOriginAllowedToAllowWildcardSubdomains()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Use Clean Architecture Infrastructure Injection
builder.Services.AddInfrastructure(builder.Configuration);

// Domain / Application layer registrations
builder.Services.AddScoped<SpeedAlert.Domain.Services.OverspeedValidationEngine>();
builder.Services.AddScoped<ISpeedLimitProviderOrchestrator, SpeedLimitProviderOrchestrator>();

// Strict Secret Management
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("CRITICAL STARTUP ERROR: Missing required configuration 'Jwt:Key'!");
}
var adminEmail = builder.Configuration["Admin:Email"];
if (string.IsNullOrWhiteSpace(adminEmail))
{
    throw new InvalidOperationException("CRITICAL STARTUP ERROR: Missing required configuration 'Admin:Email'!");
}
var adminPassword = builder.Configuration["Admin:Password"];
if (string.IsNullOrWhiteSpace(adminPassword))
{
    throw new InvalidOperationException("CRITICAL STARTUP ERROR: Missing required configuration 'Admin:Password'!");
}

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "speedalert-api",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "speedalert-mobile",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        // SignalR token mapping from query string
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/telemetry"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Safe Database Migrations & Seeding
var runMigrations = Environment.GetEnvironmentVariable("RUN_MIGRATIONS") == "true" || args.Contains("--migrate");
if (runMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            Log.Information("Applying migrations explicitly as requested...");
            db.Database.Migrate();

            if (!db.Users.Any(u => u.Email == adminEmail))
            {
                Log.Information($"Seeding default admin user: {adminEmail}");
                var adminUser = new User
                {
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = "Admin",
                    IsActive = true
                };
                
                adminUser.Settings = new UserSettings { UserId = adminUser.Id };
                
                db.Users.Add(adminUser);
                db.SaveChanges();
                Log.Information("Admin user seeded successfully.");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to migrate or seed database on explicit request!");
            throw; // Fail fast if requested but fails
        }
    }
}
else
{
    Log.Information("Skipping automatic database migrations (Run with --migrate or RUN_MIGRATIONS=true to execute)");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseCors("SecureCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");
app.MapHub<TelemetryHub>("/hub/telemetry");

app.Run();
