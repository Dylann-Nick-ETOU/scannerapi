using ApiSecurityScanner.Application.UseCases;
using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ApiSecurityScanner.Tests.UseCases;

public class GetScanByIdUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnNull_WhenScanDoesNotBelongToOwner()
    {
        var repository = new Mock<IScanRepository>();
        repository
            .Setup(x => x.GetWithIssuesForOwnerAsync(It.IsAny<Guid>(), "user-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Scan?)null);

        var useCase = new GetScanByIdUseCase(repository.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), "user-b");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnScan_WhenScanBelongsToOwner()
    {
        var scan = new Scan
        {
            OwnerId = "user-a",
            Score = 85,
            SecurityIssues =
            [
                new SecurityIssue
                {
                    RuleCode = "API-AUTH-001",
                    Endpoint = "GET /admin/users",
                    OpenApiLocation = "/paths/~1admin~1users/get",
                    Title = "Endpoint sans authentification",
                    Description = "Test issue",
                    Recommendation = "Ajouter JWT/OAuth2.",
                    OwaspCategory = "Broken Authentication",
                    OwaspTop10Id = "API2",
                    OwaspTop10Version = "2023",
                    OwaspTop10Title = "Broken Authentication"
                }
            ]
        };

        var repository = new Mock<IScanRepository>();
        repository
            .Setup(x => x.GetWithIssuesForOwnerAsync(scan.Id, "user-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(scan);

        var useCase = new GetScanByIdUseCase(repository.Object);

        var result = await useCase.ExecuteAsync(scan.Id, "user-a");

        result.Should().NotBeNull();
        result!.ScanId.Should().Be(scan.Id);
        result.Issues.Should().ContainSingle();
        result.Issues[0].OpenApiLocation.Should().Be("/paths/~1admin~1users/get");
        result.Issues[0].OwaspTop10Id.Should().Be("API2");
        result.Issues[0].OwaspTop10Version.Should().Be("2023");
        result.Issues[0].OwaspTop10Title.Should().Be("Broken Authentication");
    }
}
