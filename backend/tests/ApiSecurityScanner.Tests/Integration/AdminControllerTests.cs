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
        db.Scans.Add(new Scan
        {
            OwnerId = "member",
            TargetName = "member-api-v2",
            Score = 85,
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
    public async Task GetScanById_ShouldReturnUserScanReport_ForAdmin()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
        var scanId = db.Scans.Single(x => x.TargetName == "member-api").Id;

        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync($"/api/admin/scans/{scanId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain(scanId.ToString());
        payload.Should().Contain("\"score\":72");
    }

    [Fact]
    public async Task GetScanById_ShouldReturnNotFound_WhenScanDoesNotExist()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync($"/api/admin/scans/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompareScans_ShouldReturnComparison_ForAdmin()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
        var baselineScanId = db.Scans.Single(x => x.TargetName == "member-api").Id;
        var currentScanId = db.Scans.Single(x => x.TargetName == "member-api-v2").Id;

        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync($"/api/admin/scans/{currentScanId}/compare/{baselineScanId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain(currentScanId.ToString());
        payload.Should().Contain(baselineScanId.ToString());
        payload.Should().Contain("\"scoreDelta\":13");
    }

    [Fact]
    public async Task GetAuditLogs_ShouldReturnRecentAdminActions()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var deactivateResponse = await client.PostAsync("/api/admin/users/member/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await client.GetAsync("/api/admin/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("DeactivateUser");
        payload.Should().Contain("member");
    }

    [Fact]
    public async Task GetScanById_ShouldWriteAdminAuditLog()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
        var scanId = db.Scans.Single(x => x.TargetName == "member-api").Id;

        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync($"/api/admin/scans/{scanId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
        verificationDb.AdminAuditLogs.Should().ContainSingle(x =>
            x.ActionType == "ViewUserScanReport" &&
            x.AdminUsername == "admin" &&
            x.TargetScanId == scanId);
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
