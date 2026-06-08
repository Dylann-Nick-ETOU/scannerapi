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
                var weakRequestBodyLocation = FindWeakRequestBodyLocation(path, operation.Key, operation.Value);
                if (weakRequestBodyLocation is not null)
                {
                    issues.Add(new SecurityIssue
                    {
                        RuleCode = RuleCodeValue,
                        Severity = SeverityLevel.Medium,
                        Endpoint = endpoint,
                        OpenApiLocation = weakRequestBodyLocation,
                        Title = "Validation d'entrée insuffisante",
                        Description = "Les schémas de requête ne définissent pas assez de contraintes de validation (required, min/max, format, pattern).",
                        Recommendation = "Ajouter une validation stricte avec FluentValidation.",
                        OwaspCategory = "Improper Input Validation"
                    });
                }

                issues.AddRange(GetWeakParameterIssues(path, operation.Key, pathItem, operation.Value, endpoint));
            }
        }

        return issues;
    }

    private static string? FindWeakRequestBodyLocation(string path, OperationType operationType, OpenApiOperation operation)
    {
        if (operation.RequestBody?.Content is null)
        {
            return null;
        }

        foreach (var mediaType in operation.RequestBody.Content)
        {
            var schema = mediaType.Value.Schema;
            if (schema is null)
            {
                continue;
            }

            var pointer = OpenApiJsonPointer.ForOperation(
                path,
                operationType,
                "requestBody",
                "content",
                mediaType.Key,
                "schema");

            var weakLocation = FindWeakSchemaLocation(schema, pointer, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            if (weakLocation is not null)
            {
                return weakLocation;
            }
        }

        return null;
    }

    private static IEnumerable<SecurityIssue> GetWeakParameterIssues(
        string path,
        OperationType operationType,
        OpenApiPathItem pathItem,
        OpenApiOperation operation,
        string endpoint)
    {
        foreach (var parameter in MergeParameters(path, operationType, pathItem, operation))
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
                    OpenApiLocation = parameter.LocationPointer,
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
                OpenApiLocation = OpenApiJsonPointer.Append(parameter.LocationPointer, "schema"),
                Title = $"Paramètre {locationLabel} peu contraint",
                Description = $"Le paramètre '{parameter.Name}' n'expose pas assez de contraintes de validation dans sa définition OpenAPI.",
                Recommendation = $"Ajouter des contraintes OpenAPI sur '{parameter.Name}' ({locationLabel}) : min/max, minLength/maxLength, pattern, enum, format.",
                OwaspCategory = "Improper Input Validation"
            };
        }
    }

    private static IEnumerable<OpenApiParameterTarget> MergeParameters(
        string path,
        OperationType operationType,
        OpenApiPathItem pathItem,
        OpenApiOperation operation)
    {
        var merged = new Dictionary<(string Name, ParameterLocation? Location), OpenApiParameterTarget>();

        for (var index = 0; index < pathItem.Parameters.Count; index++)
        {
            var parameter = pathItem.Parameters[index];
            merged[(parameter.Name, parameter.In)] = new OpenApiParameterTarget(
                parameter,
                OpenApiJsonPointer.Create("paths", path, "parameters", index.ToString()));
        }

        for (var index = 0; index < operation.Parameters.Count; index++)
        {
            var parameter = operation.Parameters[index];
            merged[(parameter.Name, parameter.In)] = new OpenApiParameterTarget(
                parameter,
                OpenApiJsonPointer.ForOperation(path, operationType, "parameters", index.ToString()));
        }

        return merged.Values;
    }

    private static bool HasWeakParameterValidation(OpenApiParameterTarget parameter)
    {
        var schema = parameter.Schema;
        if (schema is null)
        {
            return false;
        }

        return !HasAnySchemaConstraint(schema) || FindWeakSchemaLocation(schema, string.Empty, new HashSet<string>(StringComparer.OrdinalIgnoreCase)) is not null;
    }

    private static string? FindWeakSchemaLocation(OpenApiSchema schema, string pointer, HashSet<string> visitedRefs)
    {
        if (schema.Reference?.Id is { Length: > 0 } referenceId)
        {
            if (!visitedRefs.Add(referenceId))
            {
                return null;
            }
        }

        foreach (var property in schema.Properties)
        {
            var propertyPointer = OpenApiJsonPointer.Append(pointer, "properties", property.Key);

            if (!HasAnySchemaConstraint(property.Value))
            {
                return propertyPointer;
            }

            var nestedWeakLocation = FindWeakSchemaLocation(property.Value, propertyPointer, visitedRefs);
            if (nestedWeakLocation is not null)
            {
                return nestedWeakLocation;
            }
        }

        if (schema.Type == "object" && schema.Properties.Count > 0 && schema.Required.Count == 0)
        {
            return pointer;
        }

        if (schema.Items is not null)
        {
            var itemsWeakLocation = FindWeakSchemaLocation(
                schema.Items,
                OpenApiJsonPointer.Append(pointer, "items"),
                visitedRefs);

            if (itemsWeakLocation is not null)
            {
                return itemsWeakLocation;
            }
        }

        foreach (var (segment, compositeList) in new[]
                 {
                     ("allOf", schema.AllOf),
                     ("anyOf", schema.AnyOf),
                     ("oneOf", schema.OneOf)
                 })
        {
            for (var index = 0; index < compositeList.Count; index++)
            {
                var compositeWeakLocation = FindWeakSchemaLocation(
                    compositeList[index],
                    OpenApiJsonPointer.Append(pointer, segment, index.ToString()),
                    visitedRefs);

                if (compositeWeakLocation is not null)
                {
                    return compositeWeakLocation;
                }
            }
        }

        return null;
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

    private sealed record OpenApiParameterTarget(OpenApiParameter Parameter, string LocationPointer)
    {
        public string Name => Parameter.Name;
        public ParameterLocation? In => Parameter.In;
        public bool Required => Parameter.Required;
        public OpenApiSchema? Schema => Parameter.Schema;
    }
}
