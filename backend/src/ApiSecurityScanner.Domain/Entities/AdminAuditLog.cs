namespace ApiSecurityScanner.Domain.Entities;

public class AdminAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AdminUsername { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? TargetUsername { get; set; }
    public Guid? TargetScanId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
