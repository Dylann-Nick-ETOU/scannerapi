namespace ApiSecurityScanner.Application.DTOs;

public class ScanHistoryItemDto
{
    public Guid Id { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string? OpenApiUrl { get; set; }
    public int Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int IssuesCount { get; set; }
}
