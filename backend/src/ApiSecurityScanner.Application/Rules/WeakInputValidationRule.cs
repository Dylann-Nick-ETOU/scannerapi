using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

public class WeakInputValidationRule : ISecurityRule
{
    public string RuleCode => "API-VALID-001";
    public string Name => "Weak Input Validation";

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
                var hasWeakValidation = false;

                if (operation.Value.RequestBody?.Content is not null)
                {
                    foreach (var mediaType in operation.Value.RequestBody.Content.Values)
                    {
                        var schema = mediaType.Schema;
                        if (schema is null)
                        {
                            continue;
                        }

                        if (HasWeakValidation(schema, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
                        {
                            hasWeakValidation = true;
                            break;
                        }
                    }
                }

                if (!hasWeakValidation)
                {
                    continue;
                }

                issues.Add(new SecurityIssue
                {
                    RuleCode = RuleCode,
                    Severity = SeverityLevel.Medium,
                    Endpoint = $"{operation.Key} {path}",
                    Title = "Validation d'entrée insuffisante",
                    Description = "Les schémas de requête ne définissent pas assez de contraintes de validation (required, min/max, format, pattern).",
                    Recommendation = "Ajouter une validation stricte avec FluentValidation.",
                    OwaspCategory = "Improper Input Validation"
                });
            }
        }

        return issues;
    }

    private static bool HasWeakValidation(OpenApiSchema schema, HashSet<string> visitedRefs)
    {
        if (schema.Reference?.Id is { Length: > 0 } referenceId)
        {
            if (!visitedRefs.Add(referenceId))
            {
                return false;
            }
        }

        foreach (var property in schema.Properties.Values)
        {
            var hasAnyConstraint =
                property.MinLength.HasValue ||
                property.MaxLength.HasValue ||
                property.Pattern is { Length: > 0 } ||
                property.Minimum.HasValue ||
                property.Maximum.HasValue ||
                property.Format is { Length: > 0 };

            if (!hasAnyConstraint)
            {
                return true;
            }

            if (HasWeakValidation(property, visitedRefs))
            {
                return true;
            }
        }

        if (schema.Type == "object" && schema.Properties.Count > 0 && schema.Required.Count == 0)
        {
            return true;
        }

        if (schema.Items is not null && HasWeakValidation(schema.Items, visitedRefs))
        {
            return true;
        }

        foreach (var composite in schema.AllOf.Concat(schema.AnyOf).Concat(schema.OneOf))
        {
            if (HasWeakValidation(composite, visitedRefs))
            {
                return true;
            }
        }

        return false;
    }
}
