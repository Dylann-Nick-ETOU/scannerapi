using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class GetAllScansUseCase(IScanRepository scanRepository)
{
    public async Task<IReadOnlyList<ScanHistoryItemDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var scans = await scanRepository.GetAllWithIssuesAsync(cancellationToken);

        return scans.Select(scan => new ScanHistoryItemDto
        {
            Id = scan.Id,
            TargetName = scan.TargetName,
            OpenApiUrl = scan.OpenApiUrl,
            Score = scan.Score,
            Status = scan.Status.ToString(),
            CreatedAt = scan.CreatedAt,
            IssuesCount = scan.SecurityIssues.Count
        }).ToList();
    }
}
