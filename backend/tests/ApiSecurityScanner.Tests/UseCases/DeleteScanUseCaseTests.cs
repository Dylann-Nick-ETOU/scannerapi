using ApiSecurityScanner.Application.UseCases;
using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ApiSecurityScanner.Tests.UseCases;

public class DeleteScanUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldNotDelete_WhenScanDoesNotBelongToOwner()
    {
        var repository = new Mock<IScanRepository>();
        repository
            .Setup(x => x.GetByIdForOwnerAsync(It.IsAny<Guid>(), "user-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Scan?)null);

        var useCase = new DeleteScanUseCase(repository.Object);

        var deleted = await useCase.ExecuteAsync(Guid.NewGuid(), "user-b");

        deleted.Should().BeFalse();
        repository.Verify(x => x.DeleteAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDelete_WhenScanBelongsToOwner()
    {
        var scan = new Scan { OwnerId = "user-a" };
        var repository = new Mock<IScanRepository>();
        repository
            .Setup(x => x.GetByIdForOwnerAsync(scan.Id, "user-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(scan);

        var useCase = new DeleteScanUseCase(repository.Object);

        var deleted = await useCase.ExecuteAsync(scan.Id, "user-a");

        deleted.Should().BeTrue();
        repository.Verify(x => x.DeleteAsync(scan, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
