using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

public class InsecureServerUrlRule : ISecurityRule
{
    public string RuleCode => "API-CONFIG-001";
    public string Name => "Insecure Server URL";

    public IReadOnlyList<SecurityIssue> Evaluate(object document)
    {
        if (document is not OpenApiDocument openApi)
        {
            return [];
        }

        var issues = new List<SecurityIssue>();

        foreach (var server in openApi.Servers)
        {
            if (!Uri.TryCreate(server.Url, UriKind.Absolute, out var uri))
            {
                continue;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            issues.Add(new SecurityIssue
            {
                RuleCode = RuleCode,
                Severity = SeverityLevel.Medium,
                Endpoint = "Server URL",
                Title = "Configuration serveur non sécurisée",
                Description = $"Le serveur '{server.Url}' utilise HTTP.",
                Recommendation = "Forcer HTTPS.",
                OwaspCategory = "Security Misconfiguration"
            });
        }

        return issues;
    }
}
