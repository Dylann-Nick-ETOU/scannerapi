namespace ApiSecurityScanner.Application.DTOs;

public class ComparedScanDto
{
    public Guid ScanId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string? OpenApiUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Score { get; set; }
    public ScanSummaryDto Summary { get; set; } = new();
}
