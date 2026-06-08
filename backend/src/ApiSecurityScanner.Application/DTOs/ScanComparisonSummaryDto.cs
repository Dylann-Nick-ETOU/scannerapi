namespace ApiSecurityScanner.Application.DTOs;

public class ScanComparisonSummaryDto
{
    public int NewIssuesCount { get; set; }
    public int ResolvedIssuesCount { get; set; }
    public int UnchangedIssuesCount { get; set; }
}
