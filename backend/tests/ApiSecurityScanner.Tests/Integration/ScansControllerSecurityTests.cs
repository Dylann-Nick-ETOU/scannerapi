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
        var seeded = await SeedScanAsync("user-a");
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-a");
        client.DefaultRequestHeaders.Add("X-Test-Role", "User");

        var response = await client.DeleteAsync($"/api/scans/{seeded.ScanId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetScanById_ShouldReturnNotFound_ForAnotherOwner()
    {
        var seeded = await SeedScanAsync("user-a");
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-b");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync($"/api/scans/{seeded.ScanId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetScanById_ShouldReturnOk_ForOwner()
    {
        var seeded = await SeedScanAsync("user-a");
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-a");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync($"/api/scans/{seeded.ScanId}");

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

    [Fact]
    public async Task CompareScans_ShouldReturnDiff_ForOwner()
    {
        var baselineScanId = await SeedScanWithIssuesAsync(
            "compare-user",
            62,
            new[]
            {
                CreateIssue("API-AUTH-001", "GET /admin/users", "/paths/~1admin~1users/get", "Endpoint sans authentification"),
                CreateIssue("API-DATA-001", "GET /profile", "/paths/~1profile/get/responses/200", "Champ sensible exposé")
            });

        var currentScanId = await SeedScanWithIssuesAsync(
            "compare-user",
            78,
            new[]
            {
                CreateIssue("API-AUTH-001", "GET /admin/users", "/paths/~1admin~1users/get", "Endpoint sans authentification"),
                CreateIssue("API-INPUT-001", "GET /activities", "/paths/~1activities/get/parameters/0/schema", "Paramètre sans contrainte")
            });

        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "compare-user");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync($"/api/scans/{currentScanId}/compare/{baselineScanId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ScanComparisonResponse>();
        payload.Should().NotBeNull();
        payload!.ScoreDelta.Should().Be(16);
        payload.TotalIssuesDelta.Should().Be(0);
        payload.Summary.NewIssuesCount.Should().Be(1);
        payload.Summary.ResolvedIssuesCount.Should().Be(1);
        payload.Summary.UnchangedIssuesCount.Should().Be(1);
        payload.NewIssues.Should().ContainSingle(x => x.RuleCode == "API-INPUT-001");
        payload.ResolvedIssues.Should().ContainSingle(x => x.RuleCode == "API-DATA-001");
        payload.UnchangedIssues.Should().ContainSingle(x => x.RuleCode == "API-AUTH-001");
        payload.Current.ScanId.Should().Be(currentScanId);
        payload.Baseline.ScanId.Should().Be(baselineScanId);
    }

    [Fact]
    public async Task CompareScans_ShouldReturnNotFound_WhenOneScanIsNotOwned()
    {
        var baselineScanId = await SeedScanAsync("user-a");
        var currentScanId = await SeedScanAsync("user-b");

        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-a");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync($"/api/scans/{currentScanId.ScanId}/compare/{baselineScanId.ScanId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateIssueReview_ShouldPersistAcceptedRisk_ForOwner()
    {
        var seeded = await SeedScanAsync("user-a");
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-a");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/scans/{seeded.ScanId}/issues/{seeded.IssueId}/review")
        {
            Content = JsonContent.Create(new
            {
                status = "AcceptedRisk",
                comment = "Déjà couvert par un contrôle amont."
            })
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ReviewedIssueResponse>();
        payload.Should().NotBeNull();
        payload!.ReviewStatus.Should().Be("AcceptedRisk");
        payload.ReviewComment.Should().Be("Déjà couvert par un contrôle amont.");
        payload.ReviewedBy.Should().Be("user-a");
        payload.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateIssueReview_ShouldReturnNotFound_ForAnotherOwner()
    {
        var seeded = await SeedScanAsync("user-a");
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-b");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/scans/{seeded.ScanId}/issues/{seeded.IssueId}/review")
        {
            Content = JsonContent.Create(new
            {
                status = "FalsePositive"
            })
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateIssueReview_ShouldReturnBadRequest_ForUnsupportedStatus()
    {
        var seeded = await SeedScanAsync("user-a");
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-a");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/scans/{seeded.ScanId}/issues/{seeded.IssueId}/review")
        {
            Content = JsonContent.Create(new
            {
                status = "Ignored"
            })
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<SeededScan> SeedScanAsync(string ownerId)
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

        var issue = new SecurityIssue
        {
            RuleCode = "API-AUTH-001",
            Endpoint = "GET /admin/users",
            Title = "Endpoint sans authentification",
            Description = "Seed issue",
            Recommendation = "Ajouter JWT/OAuth2.",
            OwaspCategory = "Broken Authentication"
        };

        scan.SecurityIssues.Add(issue);

        db.Scans.Add(scan);
        await db.SaveChangesAsync();
        return new SeededScan(scan.Id, issue.Id);
    }

    private async Task<Guid> SeedScanWithIssuesAsync(string ownerId, int score, IEnumerable<SecurityIssue> issues)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
        var scan = new Scan
        {
            OwnerId = ownerId,
            TargetName = "seeded-api",
            OpenApiUrl = "https://example.com/openapi.json",
            Score = score,
            Status = Domain.Enums.ScanStatus.Completed
        };

        foreach (var issue in issues)
        {
            scan.SecurityIssues.Add(issue);
        }

        db.Scans.Add(scan);
        await db.SaveChangesAsync();
        return scan.Id;
    }

    private static SecurityIssue CreateIssue(string ruleCode, string endpoint, string openApiLocation, string title) =>
        new()
        {
            RuleCode = ruleCode,
            Endpoint = endpoint,
            OpenApiLocation = openApiLocation,
            Title = title,
            Description = "Seed issue",
            Recommendation = "Seed recommendation",
            OwaspCategory = "Seed category"
        };

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    private sealed record SeededScan(Guid ScanId, Guid IssueId);

    private sealed class ReviewedIssueResponse
    {
        public string ReviewStatus { get; set; } = string.Empty;
        public string ReviewComment { get; set; } = string.Empty;
        public string ReviewedBy { get; set; } = string.Empty;
        public DateTime? ReviewedAt { get; set; }
    }

    private sealed class ScanComparisonResponse
    {
        public ComparedScanResponse Baseline { get; set; } = new();
        public ComparedScanResponse Current { get; set; } = new();
        public int ScoreDelta { get; set; }
        public int TotalIssuesDelta { get; set; }
        public ScanComparisonSummaryResponse Summary { get; set; } = new();
        public List<ComparedIssueResponse> NewIssues { get; set; } = [];
        public List<ComparedIssueResponse> ResolvedIssues { get; set; } = [];
        public List<ComparedIssueResponse> UnchangedIssues { get; set; } = [];
    }

    private sealed class ComparedScanResponse
    {
        public Guid ScanId { get; set; }
    }

    private sealed class ScanComparisonSummaryResponse
    {
        public int NewIssuesCount { get; set; }
        public int ResolvedIssuesCount { get; set; }
        public int UnchangedIssuesCount { get; set; }
    }

    private sealed class ComparedIssueResponse
    {
        public string RuleCode { get; set; } = string.Empty;
    }
}
