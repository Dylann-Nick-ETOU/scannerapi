using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class CompareAdminScansUseCase(IScanRepository scanRepository)
{
    public async Task<ScanComparisonDto?> ExecuteAsync(
        Guid currentScanId,
        Guid baselineScanId,
        CancellationToken cancellationToken = default)
    {
        var currentScan = await scanRepository.GetWithIssuesAsync(currentScanId, cancellationToken);
        if (currentScan is null)
        {
            return null;
        }

        var baselineScan = await scanRepository.GetWithIssuesAsync(baselineScanId, cancellationToken);
        if (baselineScan is null)
        {
            return null;
        }

        return currentScan.ToComparisonDto(baselineScan);
    }
}
