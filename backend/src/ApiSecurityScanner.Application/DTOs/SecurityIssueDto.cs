namespace ApiSecurityScanner.Application.DTOs;

public class SecurityIssueDto
{
    public Guid Id { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string DetectionConfidence { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
    public string ReviewComment { get; set; } = string.Empty;
    public DateTime? ReviewedAt { get; set; }
    public string ReviewedBy { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string OpenApiLocation { get; set; } = string.Empty;
    public string OpenApiExcerpt { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string OwaspCategory { get; set; } = string.Empty;
    public string OwaspTop10Id { get; set; } = string.Empty;
    public string OwaspTop10Version { get; set; } = string.Empty;
    public string OwaspTop10Title { get; set; } = string.Empty;
}
