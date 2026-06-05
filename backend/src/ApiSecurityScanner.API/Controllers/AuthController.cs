using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiSecurityScanner.API.Authentication;
using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ApiSecurityScanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IConfiguration configuration,
    IUserRepository userRepository,
    PasswordHasher<string> passwordHasher) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        var user = await userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "This account is disabled." });
        }

        var result = passwordHasher.VerifyHashedPassword(user.Username, user.PasswordHash, request.Password);
        if (result is PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await userRepository.SaveChangesAsync(cancellationToken);

        return Ok(CreateAuthResponse(user.Username, user.Role));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new { message = "Password confirmation does not match." });
        }

        if (request.Username.Length < 3 || request.Username.Length > 100)
        {
            return BadRequest(new { message = "Username must contain between 3 and 100 characters." });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new { message = "Password must contain at least 8 characters." });
        }

        if (await userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
        {
            return Conflict(new { message = "This username is already in use." });
        }

        var username = request.Username.Trim();
        var user = new AppUser
        {
            Username = username,
            Role = "User",
            IsActive = true,
            PasswordHash = passwordHasher.HashPassword(username, request.Password)
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return Ok(CreateAuthResponse(user.Username, user.Role));
    }

    private object CreateAuthResponse(string username, string role)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "ApiSecurityScanner";
        var audience = configuration["Jwt:Audience"] ?? "ApiSecurityScanner.Frontend";
        var signingKey = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Missing Jwt:SigningKey");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(ClaimTypes.NameIdentifier, username),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);

        return new { accessToken = encoded, tokenType = "Bearer", expiresIn = 8 * 3600, username, role };
    }
}
