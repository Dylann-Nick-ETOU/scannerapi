using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Application.Services;
using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class ScanOpenApiUseCase(
    IOpenApiDocumentLoader documentLoader,
    SecurityRuleEngine ruleEngine,
    ScanScoringService scoringService,
    IScanRepository scanRepository)
{
    public async Task<ScanReportDto> ExecuteAsync(ScanRequestDto request, string ownerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OpenApiUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var document = await documentLoader.LoadFromUrlAsync(request.OpenApiUrl, cancellationToken);
        var issues = ruleEngine.Analyze(document).ToList();

        var scan = new Scan
        {
            OwnerId = ownerId,
            TargetName = request.TargetName ?? new Uri(request.OpenApiUrl).Host,
            OpenApiUrl = request.OpenApiUrl,
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
                OpenApiLocation = x.OpenApiLocation,
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
