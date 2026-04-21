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

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Use Clean Architecture Infrastructure Injection
builder.Services.AddInfrastructure(builder.Configuration);

// Domain / Application layer registrations (Overspeed engine doesn't need constructor injection but we can register it)
builder.Services.AddScoped<SpeedAlert.Domain.Services.OverspeedValidationEngine>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "Generate_A_Random_Secure_String_Min_32_Chars!";
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
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Migrate DB and Seed Admin User on startup (Railway friendly)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Log.Information("Applying migrations...");
        db.Database.Migrate();

        // Seed Admin Data
        var adminEmail = builder.Configuration["Admin:Email"] ?? "admin@speedalert.com";
        var adminPassword = builder.Configuration["Admin:Password"] ?? "AdminSecure123!";

        if (!db.Users.Any(u => u.Email == adminEmail))
        {
            Log.Information($"Seeding default admin user: {adminEmail}");
            var adminUser = new User
            {
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = "Admin", // Crucial for [Authorize(Roles = "Admin")]
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
        Log.Error(ex, "Failed to migrate or seed database on startup");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseForwardedHeaders();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

app.Run();
