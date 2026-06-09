using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

public class SensitiveDataExposureRule : ISecurityRule
{
    private static readonly OwaspTop10Mapping Mapping = OwaspTop10Mappings.ExcessiveDataExposure2019;
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

                    foreach (var mediaType in response.Value.Content)
                    {
                        var schema = mediaType.Value.Schema;
                        if (schema is null)
                        {
                            continue;
                        }

                        var location = OpenApiJsonPointer.ForOperation(
                            path,
                            operation.Key,
                            "responses",
                            response.Key,
                            "content",
                            mediaType.Key,
                            "schema");

                        var exposed = FindSensitiveFields(
                            schema,
                            currentPath: string.Empty,
                            pointer: location,
                            visitedRefs: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

                        foreach (var field in exposed)
                        {
                            issues.Add(new SecurityIssue
                            {
                                RuleCode = RuleCode,
                                Severity = SeverityLevel.Critical,
                                DetectionConfidence = DetectionConfidenceLevels.High,
                                Endpoint = $"{operation.Key} {path}",
                                OpenApiLocation = field.Pointer,
                                OpenApiExcerpt = OpenApiExcerptFormatter.ForSchema(field.FieldPath, field.Schema),
                                Title = "Champ sensible exposé",
                                Description = $"Le champ sensible '{field.FieldPath}' est présent dans un schéma de réponse.",
                                Recommendation = "Ne jamais exposer ces champs dans les DTO de sortie.",
                                OwaspCategory = Mapping.Title,
                                OwaspTop10Id = Mapping.Id,
                                OwaspTop10Version = Mapping.Version,
                                OwaspTop10Title = Mapping.Title
                            });
                        }
                    }
                }
            }
        }

        return issues;
    }

    private static HashSet<SensitiveFieldMatch> FindSensitiveFields(
        OpenApiSchema schema,
        string currentPath,
        string pointer,
        HashSet<string> visitedRefs)
    {
        var found = new HashSet<SensitiveFieldMatch>();

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
            var propertyPointer = OpenApiJsonPointer.Append(pointer, "properties", property.Key);

            if (SensitiveFields.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
            {
                found.Add(new SensitiveFieldMatch(propertyPath, propertyPointer, property.Value));
            }

            foreach (var nested in FindSensitiveFields(property.Value, propertyPath, propertyPointer, visitedRefs))
            {
                found.Add(nested);
            }
        }

        if (schema.Items is not null)
        {
            var itemsPath = string.IsNullOrWhiteSpace(currentPath) ? "[]" : $"{currentPath}[]";
            var itemsPointer = OpenApiJsonPointer.Append(pointer, "items");
            foreach (var nested in FindSensitiveFields(schema.Items, itemsPath, itemsPointer, visitedRefs))
            {
                found.Add(nested);
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
                var compositePointer = OpenApiJsonPointer.Append(pointer, segment, index.ToString());
                foreach (var nested in FindSensitiveFields(compositeList[index], currentPath, compositePointer, visitedRefs))
                {
                    found.Add(nested);
                }
            }
        }

        return found;
    }

    private sealed record SensitiveFieldMatch(string FieldPath, string Pointer, OpenApiSchema Schema);
}
