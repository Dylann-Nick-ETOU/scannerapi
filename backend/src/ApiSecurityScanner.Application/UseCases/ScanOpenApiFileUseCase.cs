using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Application.Services;
using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class ScanOpenApiFileUseCase(
    IOpenApiDocumentLoader documentLoader,
    SecurityRuleEngine ruleEngine,
    ScanScoringService scoringService,
    IScanRepository scanRepository)
{
    public async Task<ScanReportDto> ExecuteAsync(ScanFileRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileContent);

        var document = documentLoader.LoadFromText(request.FileContent);
        var issues = ruleEngine.Analyze(document).ToList();

        var scan = new Scan
        {
            TargetName = request.TargetName ?? "uploaded-openapi-file",
            OpenApiUrl = null,
            Status = ScanStatus.Completed,
            Score = scoringService.ComputeScore(issues)
        };

        foreach (var issue in issues)
        {
            issue.ScanId = scan.Id;
            scan.SecurityIssues.Add(issue);
        }

        await scanRepository.AddAsync(scan, cancellationToken);
        await scanRepository.SaveChangesAsync(cancellationToken);

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
