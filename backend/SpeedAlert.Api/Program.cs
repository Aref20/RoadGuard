using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SpeedAlert.Api.Hubs;
using SpeedAlert.Api.Services;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Application.Services;
using SpeedAlert.Domain.Entities;
using SpeedAlert.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/speedalert-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddHostedService<TelemetryBroadcastService>();

var configuredAllowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var allowedOrigins = configuredAllowedOrigins
    .SelectMany(origin => origin.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .Where(origin => !string.IsNullOrWhiteSpace(origin) && origin != "*")
    .Concat(
    [
        "http://localhost:3000",
        "http://localhost:3001",
        "https://roadguard-production.up.railway.app",
        "https://*.up.railway.app"
    ])
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

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<SpeedAlert.Domain.Services.OverspeedValidationEngine>();
builder.Services.AddScoped<ISpeedLimitProviderOrchestrator, SpeedLimitProviderOrchestrator>();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("CRITICAL STARTUP ERROR: Missing required configuration 'Jwt:Key'.");
}

var adminEmail = builder.Configuration["Admin:Email"];
if (string.IsNullOrWhiteSpace(adminEmail))
{
    throw new InvalidOperationException("CRITICAL STARTUP ERROR: Missing required configuration 'Admin:Email'.");
}

var adminPassword = builder.Configuration["Admin:Password"];
if (string.IsNullOrWhiteSpace(adminPassword))
{
    throw new InvalidOperationException("CRITICAL STARTUP ERROR: Missing required configuration 'Admin:Password'.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "speedalert-api",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "speedalert-clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hub/telemetry"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    context.Fail("AUTH_UNAUTHORIZED");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<IAppDbContext>();
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId);
                if (user == null)
                {
                    context.Fail("AUTH_UNAUTHORIZED");
                    return;
                }

                if (!user.IsActive)
                {
                    context.Fail("AUTH_ACCOUNT_DISABLED");
                }
            },
            OnChallenge = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var code = context.AuthenticateFailure?.Message == "AUTH_ACCOUNT_DISABLED"
                    ? "AUTH_ACCOUNT_DISABLED"
                    : "AUTH_UNAUTHORIZED";
                var message = code == "AUTH_ACCOUNT_DISABLED"
                    ? "Account is disabled."
                    : "Authentication is required.";

                await context.Response.WriteAsJsonAsync(new { code, message });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "AUTH_FORBIDDEN",
                    message = "You do not have permission to access this resource."
                });
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

var runMigrations = Environment.GetEnvironmentVariable("RUN_MIGRATIONS") == "true" || args.Contains("--migrate");
await ApplicationInitializationService.InitializeAsync(app.Services, runMigrations, adminEmail, adminPassword);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseSerilogRequestLogging();
app.UseCors("SecureCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");
app.MapHub<TelemetryHub>("/hub/telemetry").RequireAuthorization("AdminOnly");

await app.RunAsync();

public partial class Program;
