using ApiSecurityScanner.Application.Services;
using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using FluentAssertions;

namespace ApiSecurityScanner.Tests.Services;

public class ScanScoringServiceTests
{
    [Fact]
    public void ComputeScore_ShouldReturn100_WhenNoIssue()
    {
        var service = new ScanScoringService();
        var score = service.ComputeScore([]);
        score.Should().Be(100);
    }

    [Fact]
    public void ComputeScore_ShouldApplyPenalties()
    {
        var service = new ScanScoringService();
        var issues = new List<SecurityIssue>
        {
            new() { Severity = SeverityLevel.Critical },
            new() { Severity = SeverityLevel.High },
            new() { Severity = SeverityLevel.Medium },
            new() { Severity = SeverityLevel.Low }
        };

        var score = service.ComputeScore(issues);
        score.Should().Be(49);
    }
}
