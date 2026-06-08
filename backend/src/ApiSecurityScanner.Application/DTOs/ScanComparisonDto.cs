namespace ApiSecurityScanner.Application.DTOs;

public class ScanComparisonDto
{
    public ComparedScanDto Baseline { get; set; } = new();
    public ComparedScanDto Current { get; set; } = new();
    public int ScoreDelta { get; set; }
    public int TotalIssuesDelta { get; set; }
    public ScanComparisonSummaryDto Summary { get; set; } = new();
    public List<SecurityIssueDto> NewIssues { get; set; } = new();
    public List<SecurityIssueDto> ResolvedIssues { get; set; } = new();
    public List<SecurityIssueDto> UnchangedIssues { get; set; } = new();
}
