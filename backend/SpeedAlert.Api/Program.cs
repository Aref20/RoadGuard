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

// Migrate DB on startup (Railway friendly)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to migrate database on startup");
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
