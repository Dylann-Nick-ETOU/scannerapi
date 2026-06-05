using ApiSecurityScanner.Domain.Enums;

namespace ApiSecurityScanner.Domain.Entities;

public class Scan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string? OpenApiUrl { get; set; }
    public int Score { get; set; }
    public ScanStatus Status { get; set; } = ScanStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<SecurityIssue> SecurityIssues { get; set; } = new List<SecurityIssue>();
}
