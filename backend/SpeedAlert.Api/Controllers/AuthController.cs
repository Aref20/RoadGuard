using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using SpeedAlert.Application.Interfaces;
using SpeedAlert.Domain.Entities;
using BCrypt.Net;
using System.Threading.Tasks;
using System.Linq;
using System;

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
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new { message = "Email inside use" });
            
        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
        
        user.Settings = new UserSettings { UserId = user.Id };
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        
        return Ok(new { token = GenerateJwt(user) });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });
            
        return Ok(new { token = GenerateJwt(user) });
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class AuthDto { public string Email { get; set; } = null!; public string Password { get; set; } = null!; }
