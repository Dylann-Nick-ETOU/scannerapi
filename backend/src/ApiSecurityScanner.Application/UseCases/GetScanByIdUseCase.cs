using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class GetScanByIdUseCase(IScanRepository scanRepository)
{
    public async Task<ScanReportDto?> ExecuteAsync(Guid id, string ownerId, CancellationToken cancellationToken = default)
    {
        var scan = await scanRepository.GetWithIssuesForOwnerAsync(id, ownerId, cancellationToken);
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
                DetectionConfidence = x.DetectionConfidence,
                Endpoint = x.Endpoint,
                OpenApiLocation = x.OpenApiLocation,
                OpenApiExcerpt = x.OpenApiExcerpt,
                Title = x.Title,
                Description = x.Description,
                Recommendation = x.Recommendation,
                OwaspCategory = x.OwaspCategory,
                OwaspTop10Id = x.OwaspTop10Id,
                OwaspTop10Version = x.OwaspTop10Version,
                OwaspTop10Title = x.OwaspTop10Title
            }).ToList()
        };
    }
}
