using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;

namespace ApiSecurityScanner.Application.Services;

public class ScanScoringService
{
    public int ComputeScore(IReadOnlyCollection<SecurityIssue> issues)
    {
        var penalty = issues.Sum(issue => issue.Severity switch
        {
            SeverityLevel.Critical => 25,
            SeverityLevel.High => 15,
            SeverityLevel.Medium => 8,
            _ => 3
        });

        return Math.Clamp(100 - penalty, 0, 100);
    }
}
