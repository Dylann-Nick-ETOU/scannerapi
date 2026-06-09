using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

public class MissingAuthenticationRule : ISecurityRule
{
    private static readonly OwaspTop10Mapping Mapping = OwaspTop10Mappings.BrokenAuthentication2023;
    public string RuleCode => "API-AUTH-001";
    public string Name => "Missing Authentication";

    public IReadOnlyList<SecurityIssue> Evaluate(object document)
    {
        if (document is not OpenApiDocument openApi)
        {
            return [];
        }

        var issues = new List<SecurityIssue>();

        foreach (var (path, pathItem) in openApi.Paths)
        {
            foreach (var operation in pathItem.Operations)
            {
                var hasOperationSecurity = operation.Value.Security is { Count: > 0 };
                var hasGlobalSecurity = openApi.SecurityRequirements is { Count: > 0 };
                if (hasOperationSecurity || hasGlobalSecurity)
                {
                    continue;
                }

                var location = OpenApiJsonPointer.ForOperation(path, operation.Key);

                issues.Add(new SecurityIssue
                {
                    RuleCode = RuleCode,
                    Severity = SeverityLevel.High,
                    DetectionConfidence = DetectionConfidenceLevels.High,
                    Endpoint = $"{operation.Key} {path}",
                    OpenApiLocation = location,
                    OpenApiExcerpt = OpenApiExcerptFormatter.ForOperation(path, operation.Key, hasOperationSecurity, hasGlobalSecurity),
                    Title = "Endpoint sans authentification",
                    Description = "Cet endpoint semble exposé sans mécanisme d'authentification.",
                    Recommendation = "Ajouter JWT/OAuth2.",
                    OwaspCategory = Mapping.Title,
                    OwaspTop10Id = Mapping.Id,
                    OwaspTop10Version = Mapping.Version,
                    OwaspTop10Title = Mapping.Title
                });
            }
        }

        return issues;
    }
}
