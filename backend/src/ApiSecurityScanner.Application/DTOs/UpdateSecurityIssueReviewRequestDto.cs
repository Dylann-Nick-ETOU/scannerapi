namespace ApiSecurityScanner.Application.DTOs;

public class UpdateSecurityIssueReviewRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
}
