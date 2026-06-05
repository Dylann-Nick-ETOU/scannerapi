using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ApiSecurityScanner.Tests.Integration;

public class AuthControllerTests(ApiSecurityScannerApiFactory factory) : IClassFixture<ApiSecurityScannerApiFactory>
{
    [Fact]
    public async Task Login_ShouldReturnToken_ForValidCredentials()
    {
        using var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "Admin123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LoginPayload>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_ForInvalidCredentials()
    {
        using var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_ShouldCreateAccount_AndReturnToken()
    {
        using var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"user-{Guid.NewGuid():N}"[..12],
            password = "StrongPass123!",
            confirmPassword = "StrongPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LoginPayload>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.Role.Should().Be("User");
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenAccountIsDisabled()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScanner.Infrastructure.Persistence.ApiSecurityScannerDbContext>();
        var user = db.AppUsers.First(x => x.Username == "admin");
        user.IsActive = false;
        await db.SaveChangesAsync();

        using var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "Admin123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class LoginPayload
    {
        public string AccessToken { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
