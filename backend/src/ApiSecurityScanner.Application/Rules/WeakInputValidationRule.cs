using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

public class WeakInputValidationRule : ISecurityRule
{
    private const string RuleCodeValue = "API-VALID-001";
    public string RuleCode => RuleCodeValue;
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
                var endpoint = $"{operation.Key} {path}";

                if (HasWeakRequestBodyValidation(operation.Value))
                {
                    issues.Add(new SecurityIssue
                    {
                        RuleCode = RuleCodeValue,
                        Severity = SeverityLevel.Medium,
                        Endpoint = endpoint,
                        Title = "Validation d'entrée insuffisante",
                        Description = "Les schémas de requête ne définissent pas assez de contraintes de validation (required, min/max, format, pattern).",
                        Recommendation = "Ajouter une validation stricte avec FluentValidation.",
                        OwaspCategory = "Improper Input Validation"
                    });
                }

                issues.AddRange(GetWeakParameterIssues(pathItem, operation.Value, endpoint));
            }
        }

        return issues;
    }

    private static bool HasWeakRequestBodyValidation(OpenApiOperation operation)
    {
        if (operation.RequestBody?.Content is null)
        {
            return false;
        }

        foreach (var mediaType in operation.RequestBody.Content.Values)
        {
            var schema = mediaType.Schema;
            if (schema is null)
            {
                continue;
            }

            if (HasWeakSchemaValidation(schema, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<SecurityIssue> GetWeakParameterIssues(
        OpenApiPathItem pathItem,
        OpenApiOperation operation,
        string endpoint)
    {
        foreach (var parameter in MergeParameters(pathItem, operation))
        {
            var location = parameter.In;
            if (location is not ParameterLocation.Query and not ParameterLocation.Path and not ParameterLocation.Header)
            {
                continue;
            }

            var locationLabel = location.Value.ToString().ToLowerInvariant();

            if (location == ParameterLocation.Path && !parameter.Required)
            {
                yield return new SecurityIssue
                {
                    RuleCode = RuleCodeValue,
                    Severity = SeverityLevel.Medium,
                    Endpoint = endpoint,
                    Title = $"Paramètre {locationLabel} mal défini",
                    Description = $"Le paramètre '{parameter.Name}' doit être obligatoire et définir des contraintes de validation adaptées.",
                    Recommendation = $"Marquer '{parameter.Name}' comme required et ajouter des contraintes OpenAPI (pattern, enum, min/max, minLength/maxLength).",
                    OwaspCategory = "Improper Input Validation"
                };

                continue;
            }

            if (parameter.Schema is null)
            {
                continue;
            }

            if (!HasWeakParameterValidation(parameter))
            {
                continue;
            }

            yield return new SecurityIssue
            {
                RuleCode = RuleCodeValue,
                Severity = SeverityLevel.Medium,
                Endpoint = endpoint,
                Title = $"Paramètre {locationLabel} peu contraint",
                Description = $"Le paramètre '{parameter.Name}' n'expose pas assez de contraintes de validation dans sa définition OpenAPI.",
                Recommendation = $"Ajouter des contraintes OpenAPI sur '{parameter.Name}' ({locationLabel}) : min/max, minLength/maxLength, pattern, enum, format.",
                OwaspCategory = "Improper Input Validation"
            };
        }
    }

    private static IEnumerable<OpenApiParameter> MergeParameters(OpenApiPathItem pathItem, OpenApiOperation operation)
    {
        var merged = new Dictionary<(string Name, ParameterLocation? Location), OpenApiParameter>();

        foreach (var parameter in pathItem.Parameters)
        {
            merged[(parameter.Name, parameter.In)] = parameter;
        }

        foreach (var parameter in operation.Parameters)
        {
            merged[(parameter.Name, parameter.In)] = parameter;
        }

        return merged.Values;
    }

    private static bool HasWeakParameterValidation(OpenApiParameter parameter)
    {
        var schema = parameter.Schema;
        if (schema is null)
        {
            return false;
        }

        return !HasAnySchemaConstraint(schema) || HasWeakSchemaValidation(schema, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private static bool HasWeakSchemaValidation(OpenApiSchema schema, HashSet<string> visitedRefs)
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
            if (!HasAnySchemaConstraint(property))
            {
                return true;
            }

            if (HasWeakSchemaValidation(property, visitedRefs))
            {
                return true;
            }
        }

        if (schema.Type == "object" && schema.Properties.Count > 0 && schema.Required.Count == 0)
        {
            return true;
        }

        if (schema.Items is not null && HasWeakSchemaValidation(schema.Items, visitedRefs))
        {
            return true;
        }

        foreach (var composite in schema.AllOf.Concat(schema.AnyOf).Concat(schema.OneOf))
        {
            if (HasWeakSchemaValidation(composite, visitedRefs))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAnySchemaConstraint(OpenApiSchema schema)
    {
        return schema.MinLength.HasValue ||
               schema.MaxLength.HasValue ||
               schema.Pattern is { Length: > 0 } ||
               schema.Minimum.HasValue ||
               schema.Maximum.HasValue ||
               schema.ExclusiveMinimum == true ||
               schema.ExclusiveMaximum == true ||
               schema.MultipleOf.HasValue ||
               schema.Format is { Length: > 0 } ||
               schema.Enum.Count > 0 ||
               schema.MinItems.HasValue ||
               schema.MaxItems.HasValue ||
               schema.UniqueItems == true ||
               schema.MinProperties.HasValue ||
               schema.MaxProperties.HasValue;
    }
}
