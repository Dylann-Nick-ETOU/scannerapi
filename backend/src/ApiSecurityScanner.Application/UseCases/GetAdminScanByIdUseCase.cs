using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class GetAdminScanByIdUseCase(IScanRepository scanRepository)
{
    public async Task<ScanReportDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scan = await scanRepository.GetWithIssuesAsync(id, cancellationToken);
        return scan?.ToReportDto();
    }
}
