using System.Net;
using System.Net.Http.Json;
using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ApiSecurityScanner.Tests.Integration;

public class ScansControllerSecurityTests(ApiSecurityScannerApiFactory factory) : IClassFixture<ApiSecurityScannerApiFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetScans_ShouldReturnUnauthorized_WithoutAuthentication()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/scans");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteScan_ShouldReturnForbidden_ForNonAdminUser()
    {
        var scanId = await SeedScanAsync("user-a");
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-a");
        client.DefaultRequestHeaders.Add("X-Test-Role", "User");

        var response = await client.DeleteAsync($"/api/scans/{scanId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetScanById_ShouldReturnNotFound_ForAnotherOwner()
    {
        var scanId = await SeedScanAsync("user-a");
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-b");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync($"/api/scans/{scanId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetScanById_ShouldReturnOk_ForOwner()
    {
        var scanId = await SeedScanAsync("user-a");
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-a");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync($"/api/scans/{scanId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetScans_ShouldReturnTooManyRequests_WhenRateLimitIsExceeded()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "rate-limit-user");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 11; i++)
        {
            lastResponse = await client.GetAsync("/api/scans");
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ScanFromUrl_ShouldReturnBadRequest_ForBlockedSsrfTarget()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-a");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.PostAsJsonAsync("/api/scans/url", new
        {
            openApiUrl = "http://127.0.0.1/openapi.json"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("Loopback addresses are not allowed");
    }

    private async Task<Guid> SeedScanAsync(string ownerId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
        var scan = new Scan
        {
            OwnerId = ownerId,
            TargetName = "seeded-api",
            OpenApiUrl = "https://example.com/openapi.json",
            Score = 80,
            Status = Domain.Enums.ScanStatus.Completed
        };

        scan.SecurityIssues.Add(new SecurityIssue
        {
            RuleCode = "API-AUTH-001",
            Endpoint = "GET /admin/users",
            Title = "Endpoint sans authentification",
            Description = "Seed issue",
            Recommendation = "Ajouter JWT/OAuth2.",
            OwaspCategory = "Broken Authentication"
        });

        db.Scans.Add(scan);
        await db.SaveChangesAsync();
        return scan.Id;
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
