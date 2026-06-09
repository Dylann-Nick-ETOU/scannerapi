using System.Text.RegularExpressions;
using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Enums;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

public partial class InventoryManagementRule : ISecurityRule
{
    private static readonly OwaspTop10Mapping Mapping = OwaspTop10Mappings.ImproperInventoryManagement2023;
    public string RuleCode => "API-INVENTORY-001";
    public string Name => "Improper Inventory Management";

    public IReadOnlyList<SecurityIssue> Evaluate(object document)
    {
        if (document is not OpenApiDocument openApi)
        {
            return [];
        }

        var issues = new List<SecurityIssue>();
        var versionedOperations = GetVersionedOperations(openApi);
        var latestMajorVersion = versionedOperations.Count == 0
            ? (int?)null
            : versionedOperations.Max(x => x.Version.Major);

        foreach (var (path, pathItem) in openApi.Paths)
        {
            foreach (var operation in pathItem.Operations)
            {
                var endpoint = $"{operation.Key} {path}";
                var operationLocation = OpenApiJsonPointer.ForOperation(path, operation.Key);

                if (operation.Value.Deprecated)
                {
                    issues.Add(new SecurityIssue
                    {
                        RuleCode = RuleCode,
                        Severity = SeverityLevel.Medium,
                        DetectionConfidence = DetectionConfidenceLevels.High,
                        Endpoint = endpoint,
                        OpenApiLocation = OpenApiJsonPointer.Append(operationLocation, "deprecated"),
                        OpenApiExcerpt = OpenApiExcerptFormatter.ForDeprecatedOperation(path, operation.Key),
                        Title = "Endpoint obsolète encore exposé",
                        Description = "Cette opération est marquée deprecated dans la spec et reste exposée au catalogue d'API.",
                        Recommendation = "Retirer l'endpoint obsolète ou planifier sa suppression avec une date de fin de support claire.",
                        OwaspCategory = Mapping.Title,
                        OwaspTop10Id = Mapping.Id,
                        OwaspTop10Version = Mapping.Version,
                        OwaspTop10Title = Mapping.Title
                    });
                }

                if (latestMajorVersion is null)
                {
                    continue;
                }

                var endpointVersion = TryGetPathVersion(path);
                if (endpointVersion is null || endpointVersion.Major >= latestMajorVersion.Value)
                {
                    continue;
                }

                issues.Add(new SecurityIssue
                {
                    RuleCode = RuleCode,
                    Severity = SeverityLevel.Medium,
                    DetectionConfidence = DetectionConfidenceLevels.Medium,
                    Endpoint = endpoint,
                    OpenApiLocation = operationLocation,
                    OpenApiExcerpt = OpenApiExcerptFormatter.ForVersionedOperation(path, operation.Key, $"v{endpointVersion.Major}", $"v{latestMajorVersion.Value}"),
                    Title = "Ancienne version d'API encore exposée",
                    Description = $"L'endpoint appartient à la version majeure v{endpointVersion.Major} alors que la version la plus récente détectée dans la spec est v{latestMajorVersion.Value}.",
                    Recommendation = "Réduire les versions encore publiées, documenter les versions supportées et supprimer les endpoints hérités non nécessaires.",
                    OwaspCategory = Mapping.Title,
                    OwaspTop10Id = Mapping.Id,
                    OwaspTop10Version = Mapping.Version,
                    OwaspTop10Title = Mapping.Title
                });
            }
        }

        return issues;
    }

    private static List<(string Path, OperationType OperationType, ApiPathVersion Version)> GetVersionedOperations(OpenApiDocument openApi)
    {
        var operations = new List<(string Path, OperationType OperationType, ApiPathVersion Version)>();

        foreach (var (path, pathItem) in openApi.Paths)
        {
            var version = TryGetPathVersion(path);
            if (version is null)
            {
                continue;
            }

            foreach (var operation in pathItem.Operations)
            {
                operations.Add((path, operation.Key, version));
            }
        }

        return operations;
    }

    private static ApiPathVersion? TryGetPathVersion(string path)
    {
        var match = PathVersionRegex().Match(path);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["major"].Value, out var major))
        {
            return null;
        }

        var minor = 0;
        if (int.TryParse(match.Groups["minor"].Value, out var parsedMinor))
        {
            minor = parsedMinor;
        }

        return new ApiPathVersion(major, minor);
    }

    [GeneratedRegex(@"(?<=/)(v(?<major>\d+)(?:[._-](?<minor>\d+))?)(?=/|$)", RegexOptions.IgnoreCase)]
    private static partial Regex PathVersionRegex();

    private sealed record ApiPathVersion(int Major, int Minor);
}
