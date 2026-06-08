using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

internal static class OpenApiJsonPointer
{
    public static string Create(params string[] segments) =>
        "/" + string.Join("/", segments.Select(Escape));

    public static string ForOperation(string path, OperationType operationType, params string[] extraSegments) =>
        Create(["paths", path, ToOperationSegment(operationType), .. extraSegments]);

    public static string Append(string pointer, params string[] extraSegments) =>
        string.Concat(pointer, "/", string.Join("/", extraSegments.Select(Escape)));

    private static string ToOperationSegment(OperationType operationType) =>
        operationType.ToString().ToLowerInvariant();

    private static string Escape(string segment) =>
        segment.Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);
}
