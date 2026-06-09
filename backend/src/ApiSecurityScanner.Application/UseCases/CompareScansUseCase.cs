using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class CompareScansUseCase(IScanRepository scanRepository)
{
    public async Task<ScanComparisonDto?> ExecuteAsync(
        Guid currentScanId,
        Guid baselineScanId,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var currentScan = await scanRepository.GetWithIssuesForOwnerAsync(currentScanId, ownerId, cancellationToken);
        if (currentScan is null)
        {
            return null;
        }

        var baselineScan = await scanRepository.GetWithIssuesForOwnerAsync(baselineScanId, ownerId, cancellationToken);
        if (baselineScan is null)
        {
            return null;
        }

        return currentScan.ToComparisonDto(baselineScan);
    }
}
