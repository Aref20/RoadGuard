using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Domain.Entities;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SpeedAlert.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly IConfiguration _config;
    
    public AuthController(IAppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
            return BadRequest(new { message = "Email already in use" });
            
        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User",
            IsActive = true
        };
        
        user.Settings = new UserSettings { UserId = user.Id };
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        
        return Ok(new { token = GenerateJwt(user) });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });
            
        return Ok(new { token = GenerateJwt(user) });
    }

    private string GenerateJwt(User user)
    {
        var keyString = _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(keyString)) 
        {
            throw new InvalidOperationException("Jwt:Key is missing or empty.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role ?? "User")
        };
        
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "speedalert-api",
            audience: _config["Jwt:Audience"] ?? "speedalert-mobile",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class AuthDto 
{ 
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!; 

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = null!; 
}
