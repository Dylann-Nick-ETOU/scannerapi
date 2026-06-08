using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

public class MassAssignmentRule : ISecurityRule
{
    private static readonly string[] SensitiveWritableFields =
    [
        "role",
        "roles",
        "permission",
        "permissions",
        "isadmin",
        "issuperadmin",
        "ownerid",
        "tenantid",
        "accountid",
        "status",
        "isactive"
    ];

    public string RuleCode => "API-MASS-001";
    public string Name => "Mass Assignment";

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
                if (operation.Value.RequestBody?.Content is null)
                {
                    continue;
                }

                var endpoint = $"{operation.Key} {path}";
                var detectedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var mediaType in operation.Value.RequestBody.Content.Values)
                {
                    var schema = mediaType.Schema;
                    if (schema is null)
                    {
                        continue;
                    }

                    foreach (var fieldPath in FindSensitiveWritableFields(
                                 schema,
                                 currentPath: string.Empty,
                                 visitedRefs: new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
                    {
                        detectedFields.Add(fieldPath);
                    }
                }

                foreach (var fieldPath in detectedFields)
                {
                    issues.Add(new SecurityIssue
                    {
                        RuleCode = RuleCode,
                        Severity = SeverityLevel.High,
                        Endpoint = endpoint,
                        Title = "Champ sensible assignable par le client",
                        Description = $"Le champ '{fieldPath}' est accessible dans un schéma de requête et peut favoriser une faille de mass assignment.",
                        Recommendation = "Séparer les DTO d'entrée, marquer les champs internes en readOnly et filtrer explicitement les propriétés autorisées côté serveur.",
                        OwaspCategory = "Mass Assignment"
                    });
                }
            }
        }

        return issues;
    }

    private static HashSet<string> FindSensitiveWritableFields(
        OpenApiSchema schema,
        string currentPath,
        HashSet<string> visitedRefs)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (schema.ReadOnly == true)
        {
            return found;
        }

        if (schema.Reference?.Id is { Length: > 0 } referenceId)
        {
            if (!visitedRefs.Add(referenceId))
            {
                return found;
            }
        }

        foreach (var property in schema.Properties)
        {
            var propertyPath = string.IsNullOrWhiteSpace(currentPath)
                ? property.Key
                : $"{currentPath}.{property.Key}";

            if (property.Value.ReadOnly != true && IsSensitiveWritableField(property.Key))
            {
                found.Add(propertyPath);
            }

            foreach (var nested in FindSensitiveWritableFields(property.Value, propertyPath, visitedRefs))
            {
                found.Add(nested);
            }
        }

        if (schema.Items is not null)
        {
            var itemsPath = string.IsNullOrWhiteSpace(currentPath) ? "[]" : $"{currentPath}[]";
            foreach (var nested in FindSensitiveWritableFields(schema.Items, itemsPath, visitedRefs))
            {
                found.Add(nested);
            }
        }

        foreach (var composite in schema.AllOf.Concat(schema.AnyOf).Concat(schema.OneOf))
        {
            foreach (var nested in FindSensitiveWritableFields(composite, currentPath, visitedRefs))
            {
                found.Add(nested);
            }
        }

        return found;
    }

    private static bool IsSensitiveWritableField(string fieldName)
    {
        var normalized = NormalizeFieldName(fieldName);

        return SensitiveWritableFields.Contains(normalized, StringComparer.OrdinalIgnoreCase) ||
               normalized.EndsWith("role", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith("roles", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith("permission", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith("permissions", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith("ownerid", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith("tenantid", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeFieldName(string fieldName) =>
        fieldName.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim();
}
