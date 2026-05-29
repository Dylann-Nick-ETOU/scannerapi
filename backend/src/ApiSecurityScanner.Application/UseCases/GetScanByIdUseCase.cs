using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class GetScanByIdUseCase(IScanRepository scanRepository)
{
    public async Task<ScanReportDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scan = await scanRepository.GetWithIssuesAsync(id, cancellationToken);
        if (scan is null)
        {
            return null;
        }

        var issues = scan.SecurityIssues.ToList();

        return new ScanReportDto
        {
            ScanId = scan.Id,
            Score = scan.Score,
            Summary = new ScanSummaryDto
            {
                TotalIssues = issues.Count,
                Critical = issues.Count(x => x.Severity == SeverityLevel.Critical),
                High = issues.Count(x => x.Severity == SeverityLevel.High),
                Medium = issues.Count(x => x.Severity == SeverityLevel.Medium),
                Low = issues.Count(x => x.Severity == SeverityLevel.Low)
            },
            Issues = issues.Select(x => new SecurityIssueDto
            {
                RuleCode = x.RuleCode,
                Severity = x.Severity.ToString(),
                Endpoint = x.Endpoint,
                Title = x.Title,
                Description = x.Description,
                Recommendation = x.Recommendation,
                OwaspCategory = x.OwaspCategory
            }).ToList()
        };
    }
}
