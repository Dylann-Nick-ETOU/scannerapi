using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

public class SensitiveDataExposureRule : ISecurityRule
{
    private static readonly string[] SensitiveFields =
    [
        "password",
        "token",
        "refreshToken",
        "apiKey",
        "secret",
        "privateKey"
    ];

    public string RuleCode => "API-DATA-001";
    public string Name => "Sensitive Data Exposure";

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
                foreach (var response in operation.Value.Responses)
                {
                    if (response.Value.Content is null)
                    {
                        continue;
                    }

                    foreach (var mediaType in response.Value.Content.Values)
                    {
                        var schema = mediaType.Schema;
                        if (schema is null)
                        {
                            continue;
                        }

                        var exposed = FindSensitiveFields(schema, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                        foreach (var field in exposed)
                        {
                            issues.Add(new SecurityIssue
                            {
                                RuleCode = RuleCode,
                                Severity = SeverityLevel.Critical,
                                Endpoint = $"{operation.Key} {path}",
                                Title = "Champ sensible exposé",
                                Description = $"Le champ sensible '{field}' est présent dans un schéma de réponse.",
                                Recommendation = "Ne jamais exposer ces champs dans les DTO de sortie.",
                                OwaspCategory = "Sensitive Data Exposure"
                            });
                        }
                    }
                }
            }
        }

        return issues;
    }

    private static HashSet<string> FindSensitiveFields(OpenApiSchema schema, HashSet<string> visitedRefs)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (schema.Reference?.Id is { Length: > 0 } referenceId)
        {
            if (!visitedRefs.Add(referenceId))
            {
                return found;
            }
        }

        foreach (var property in schema.Properties)
        {
            if (SensitiveFields.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
            {
                found.Add(property.Key);
            }

            foreach (var nested in FindSensitiveFields(property.Value, visitedRefs))
            {
                found.Add(nested);
            }
        }

        if (schema.Items is not null)
        {
            foreach (var nested in FindSensitiveFields(schema.Items, visitedRefs))
            {
                found.Add(nested);
            }
        }

        foreach (var composite in schema.AllOf.Concat(schema.AnyOf).Concat(schema.OneOf))
        {
            foreach (var nested in FindSensitiveFields(composite, visitedRefs))
            {
                found.Add(nested);
            }
        }

        return found;
    }
}
