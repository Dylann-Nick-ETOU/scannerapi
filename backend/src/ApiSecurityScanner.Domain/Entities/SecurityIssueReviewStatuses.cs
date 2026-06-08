namespace ApiSecurityScanner.Domain.Entities;

public static class SecurityIssueReviewStatuses
{
    public const string Open = "Open";
    public const string AcceptedRisk = "AcceptedRisk";
    public const string FalsePositive = "FalsePositive";

    public static bool IsSupported(string? value) =>
        value is Open or AcceptedRisk or FalsePositive;
}
