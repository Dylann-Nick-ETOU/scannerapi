namespace ApiSecurityScanner.Application.DTOs;

public class ScanReportDto
{
    public Guid ScanId { get; set; }
    public int Score { get; set; }
    public ScanSummaryDto Summary { get; set; } = new();
    public List<SecurityIssueDto> Issues { get; set; } = new();
}
