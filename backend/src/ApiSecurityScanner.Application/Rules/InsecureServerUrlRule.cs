using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

public class InsecureServerUrlRule : ISecurityRule
{
    private static readonly OwaspTop10Mapping Mapping = OwaspTop10Mappings.SecurityMisconfiguration2023;
    public string RuleCode => "API-CONFIG-001";
    public string Name => "Insecure Server URL";

    public IReadOnlyList<SecurityIssue> Evaluate(object document)
    {
        if (document is not OpenApiDocument openApi)
        {
            return [];
        }

        var issues = new List<SecurityIssue>();

        for (var serverIndex = 0; serverIndex < openApi.Servers.Count; serverIndex++)
        {
            var server = openApi.Servers[serverIndex];
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
                OpenApiLocation = OpenApiJsonPointer.Create("servers", serverIndex.ToString(), "url"),
                Title = "Configuration serveur non sécurisée",
                Description = $"Le serveur '{server.Url}' utilise HTTP.",
                Recommendation = "Forcer HTTPS.",
                OwaspCategory = Mapping.Title,
                OwaspTop10Id = Mapping.Id,
                OwaspTop10Version = Mapping.Version,
                OwaspTop10Title = Mapping.Title
            });
        }

        return issues;
    }
}
