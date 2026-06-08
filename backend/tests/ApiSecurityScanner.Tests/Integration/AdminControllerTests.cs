using System.Net;
using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ApiSecurityScanner.Tests.Integration;

public class AdminControllerTests(ApiSecurityScannerApiFactory factory) : IClassFixture<ApiSecurityScannerApiFactory>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
        db.AppUsers.Add(new AppUser
        {
            Username = "member",
            Role = "User",
            PasswordHash = "seed"
        });
        db.Scans.Add(new Scan
        {
            OwnerId = "member",
            TargetName = "member-api",
            Score = 72,
            Status = Domain.Enums.ScanStatus.Completed
        });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetUsers_ShouldReturnForbidden_ForNonAdmin()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "member");
        client.DefaultRequestHeaders.Add("X-Test-Role", "User");

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnUsersAndActivity_ForAdmin()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("member");
        payload.Should().Contain("member-api");
    }

    [Fact]
    public async Task DeactivateUser_ShouldDisableAccount_ForAdmin()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.PostAsync("/api/admin/users/member/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
        db.AppUsers.Single(x => x.Username == "member").IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ReactivateUser_ShouldEnableAccount_ForAdmin()
    {
        using var scope = factory.Services.CreateScope();
        var seededDb = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
        seededDb.AppUsers.Single(x => x.Username == "member").IsActive = false;
        await seededDb.SaveChangesAsync();

        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.PostAsync("/api/admin/users/member/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verificationScope = factory.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
        db.AppUsers.Single(x => x.Username == "member").IsActive.Should().BeTrue();
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }
}
