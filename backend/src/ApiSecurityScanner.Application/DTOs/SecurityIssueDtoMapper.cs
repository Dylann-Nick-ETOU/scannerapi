using ApiSecurityScanner.Domain.Entities;

namespace ApiSecurityScanner.Application.DTOs;

public static class SecurityIssueDtoMapper
{
    public static SecurityIssueDto ToDto(this SecurityIssue issue) => new()
    {
        Id = issue.Id,
        RuleCode = issue.RuleCode,
        Severity = issue.Severity.ToString(),
        DetectionConfidence = issue.DetectionConfidence,
        ReviewStatus = issue.ReviewStatus,
        ReviewComment = issue.ReviewComment,
        ReviewedAt = issue.ReviewedAt,
        ReviewedBy = issue.ReviewedBy,
        Endpoint = issue.Endpoint,
        OpenApiLocation = issue.OpenApiLocation,
        OpenApiExcerpt = issue.OpenApiExcerpt,
        Title = issue.Title,
        Description = issue.Description,
        Recommendation = issue.Recommendation,
        OwaspCategory = issue.OwaspCategory,
        OwaspTop10Id = issue.OwaspTop10Id,
        OwaspTop10Version = issue.OwaspTop10Version,
        OwaspTop10Title = issue.OwaspTop10Title
    };
}
