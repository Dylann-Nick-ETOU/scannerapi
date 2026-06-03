using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ApiSecurityScanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("dev-token")]
    [AllowAnonymous]
    public IActionResult GetDevToken()
    {
        var allowTokenBootstrapInProduction =
            configuration.GetValue<bool>("Jwt:AllowTokenBootstrapInProduction");

        if (!environment.IsDevelopment() && !allowTokenBootstrapInProduction)
        {
            return NotFound();
        }

        var issuer = configuration["Jwt:Issuer"] ?? "ApiSecurityScanner";
        var audience = configuration["Jwt:Audience"] ?? "ApiSecurityScanner.Frontend";
        var signingKey = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Missing Jwt:SigningKey");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "dev-user"),
            new(ClaimTypes.Name, "dev-user"),
            new(ClaimTypes.Role, "Admin")
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

        return Ok(new { accessToken = encoded, tokenType = "Bearer", expiresIn = 8 * 3600 });
    }
}
