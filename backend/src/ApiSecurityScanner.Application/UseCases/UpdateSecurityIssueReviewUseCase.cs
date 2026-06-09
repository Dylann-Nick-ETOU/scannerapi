using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class UpdateSecurityIssueReviewUseCase(IScanRepository scanRepository)
{
    public async Task<SecurityIssueDto?> ExecuteAsync(
        Guid scanId,
        Guid issueId,
        string ownerId,
        string reviewedBy,
        UpdateSecurityIssueReviewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);
        ArgumentNullException.ThrowIfNull(request);

        var reviewStatus = NormalizeStatus(request.Status);
        var reviewComment = (request.Comment ?? string.Empty).Trim();
        if (reviewComment.Length > 1000)
        {
            throw new ArgumentException("Review comment must not exceed 1000 characters.");
        }

        var scan = await scanRepository.GetWithIssuesForOwnerAsync(scanId, ownerId, cancellationToken);
        if (scan is null)
        {
            return null;
        }

        var issue = scan.SecurityIssues.FirstOrDefault(x => x.Id == issueId);
        if (issue is null)
        {
            return null;
        }

        issue.ReviewStatus = reviewStatus;

        if (reviewStatus == SecurityIssueReviewStatuses.Open)
        {
            issue.ReviewComment = string.Empty;
            issue.ReviewedAt = null;
            issue.ReviewedBy = string.Empty;
        }
        else
        {
            issue.ReviewComment = reviewComment;
            issue.ReviewedAt = DateTime.UtcNow;
            issue.ReviewedBy = reviewedBy;
        }

        await scanRepository.SaveChangesAsync(cancellationToken);
        return issue.ToDto();
    }

    private static string NormalizeStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "open" => SecurityIssueReviewStatuses.Open,
            "acceptedrisk" or "accepted-risk" or "accepted_risk" => SecurityIssueReviewStatuses.AcceptedRisk,
            "falsepositive" or "false-positive" or "false_positive" => SecurityIssueReviewStatuses.FalsePositive,
            _ => throw new ArgumentException("Review status must be one of: Open, AcceptedRisk, FalsePositive.")
        };
    }
}
