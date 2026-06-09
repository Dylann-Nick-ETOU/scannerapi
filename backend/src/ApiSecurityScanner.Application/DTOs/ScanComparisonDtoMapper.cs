using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;

namespace ApiSecurityScanner.Application.DTOs;

public static class ScanComparisonDtoMapper
{
    public static ScanComparisonDto ToComparisonDto(this Scan currentScan, Scan baselineScan)
    {
        var currentIssues = currentScan.SecurityIssues.ToList();
        var baselineIssues = baselineScan.SecurityIssues.ToList();

        var baselineBuckets = baselineIssues
            .GroupBy(GetComparisonKey)
            .ToDictionary(group => group.Key, group => new Queue<SecurityIssue>(group));

        var newIssues = new List<SecurityIssue>();
        var unchangedIssues = new List<SecurityIssue>();

        foreach (var currentIssue in currentIssues)
        {
            if (baselineBuckets.TryGetValue(GetComparisonKey(currentIssue), out var matchingBaselineIssues)
                && matchingBaselineIssues.Count > 0)
            {
                matchingBaselineIssues.Dequeue();
                unchangedIssues.Add(currentIssue);
            }
            else
            {
                newIssues.Add(currentIssue);
            }
        }

        var resolvedIssues = baselineBuckets.Values
            .SelectMany(queue => queue)
            .ToList();

        return new ScanComparisonDto
        {
            Baseline = ToComparedScanDto(baselineScan, baselineIssues),
            Current = ToComparedScanDto(currentScan, currentIssues),
            ScoreDelta = currentScan.Score - baselineScan.Score,
            TotalIssuesDelta = currentIssues.Count - baselineIssues.Count,
            Summary = new ScanComparisonSummaryDto
            {
                NewIssuesCount = newIssues.Count,
                ResolvedIssuesCount = resolvedIssues.Count,
                UnchangedIssuesCount = unchangedIssues.Count
            },
            NewIssues = OrderIssues(newIssues).Select(issue => issue.ToDto()).ToList(),
            ResolvedIssues = OrderIssues(resolvedIssues).Select(issue => issue.ToDto()).ToList(),
            UnchangedIssues = OrderIssues(unchangedIssues).Select(issue => issue.ToDto()).ToList()
        };
    }

    private static ComparedScanDto ToComparedScanDto(Scan scan, IReadOnlyCollection<SecurityIssue> issues) => new()
    {
        ScanId = scan.Id,
        TargetName = scan.TargetName,
        OpenApiUrl = scan.OpenApiUrl,
        CreatedAt = scan.CreatedAt,
        Score = scan.Score,
        Summary = BuildSummary(issues)
    };

    private static ScanSummaryDto BuildSummary(IReadOnlyCollection<SecurityIssue> issues) => new()
    {
        TotalIssues = issues.Count,
        Critical = issues.Count(x => x.Severity == SeverityLevel.Critical),
        High = issues.Count(x => x.Severity == SeverityLevel.High),
        Medium = issues.Count(x => x.Severity == SeverityLevel.Medium),
        Low = issues.Count(x => x.Severity == SeverityLevel.Low)
    };

    private static string GetComparisonKey(SecurityIssue issue) =>
        $"{issue.RuleCode}|{issue.Endpoint}|{issue.OpenApiLocation}";

    private static IEnumerable<SecurityIssue> OrderIssues(IEnumerable<SecurityIssue> issues) =>
        issues
            .OrderByDescending(issue => issue.Severity)
            .ThenBy(issue => issue.Endpoint, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.OpenApiLocation, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.RuleCode, StringComparer.OrdinalIgnoreCase);
}
