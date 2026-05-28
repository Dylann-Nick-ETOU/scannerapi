using ApiSecurityScanner.Domain.Enums;

namespace ApiSecurityScanner.Domain.Entities;

public class SecurityIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScanId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public SeverityLevel Severity { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string OwaspCategory { get; set; } = string.Empty;
    public Scan? Scan { get; set; }
}
