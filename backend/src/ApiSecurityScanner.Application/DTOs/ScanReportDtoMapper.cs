using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;

namespace ApiSecurityScanner.Application.DTOs;

public static class ScanReportDtoMapper
{
    public static ScanReportDto ToReportDto(this Scan scan)
    {
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
            Issues = issues.Select(x => x.ToDto()).ToList()
        };
    }
}
