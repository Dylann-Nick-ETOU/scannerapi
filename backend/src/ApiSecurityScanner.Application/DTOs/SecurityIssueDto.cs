namespace ApiSecurityScanner.Application.DTOs;

public class SecurityIssueDto
{
    public string RuleCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string OwaspCategory { get; set; } = string.Empty;
}
