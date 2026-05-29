using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

public class SensitiveEndpointRule : ISecurityRule
{
    private static readonly string[] SensitiveSegments =
    [
        "/admin",
        "/users",
        "/roles",
        "/permissions",
        "/payments",
        "/orders",
        "/settings"
    ];

    public string RuleCode => "API-AUTHZ-001";
    public string Name => "Sensitive Endpoint Without Protection";

    public IReadOnlyList<SecurityIssue> Evaluate(object document)
    {
        if (document is not OpenApiDocument openApi)
        {
            return [];
        }

        var issues = new List<SecurityIssue>();

        foreach (var (path, pathItem) in openApi.Paths)
        {
            var isSensitivePath = SensitiveSegments.Any(segment => path.Contains(segment, StringComparison.OrdinalIgnoreCase));
            if (!isSensitivePath)
            {
                continue;
            }

            foreach (var operation in pathItem.Operations)
            {
                var hasOperationSecurity = operation.Value.Security is { Count: > 0 };
                var hasGlobalSecurity = openApi.SecurityRequirements is { Count: > 0 };
                if (hasOperationSecurity || hasGlobalSecurity)
                {
                    continue;
                }

                issues.Add(new SecurityIssue
                {
                    RuleCode = RuleCode,
                    Severity = SeverityLevel.High,
                    Endpoint = $"{operation.Key} {path}",
                    Title = "Endpoint sensible sans contrôle d'accès",
                    Description = "Cet endpoint sensible est accessible sans restriction de rôle/policy.",
                    Recommendation = "Ajouter rôles et policies.",
                    OwaspCategory = "Broken Access Control"
                });
            }
        }

        return issues;
    }
}
