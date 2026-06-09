using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class GetScanByIdUseCase(IScanRepository scanRepository)
{
    public async Task<ScanReportDto?> ExecuteAsync(Guid id, string ownerId, CancellationToken cancellationToken = default)
    {
        var scan = await scanRepository.GetWithIssuesForOwnerAsync(id, ownerId, cancellationToken);
        return scan?.ToReportDto();
    }
}
