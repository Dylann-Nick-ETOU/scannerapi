using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application.Rules;

internal static class OpenApiExcerptFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string ForOperation(string path, OperationType operationType, bool hasOperationSecurity, bool hasGlobalSecurity) =>
        Serialize(new
        {
            method = operationType.ToString().ToUpperInvariant(),
            path,
            security = new
            {
                operation = hasOperationSecurity,
                global = hasGlobalSecurity
            }
        });

    public static string ForServer(OpenApiServer server, string scheme) =>
        Serialize(new
        {
            url = server.Url,
            scheme
        });

    public static string ForParameter(string name, string location, bool required, OpenApiSchema? schema) =>
        Serialize(new
        {
            name,
            @in = location,
            required,
            schema = BuildSchemaExcerpt(schema)
        });

    public static string ForSchema(string? propertyName, OpenApiSchema? schema) =>
        Serialize(new
        {
            property = propertyName,
            schema = BuildSchemaExcerpt(schema)
        });

    public static string ForDeprecatedOperation(string path, OperationType operationType) =>
        Serialize(new
        {
            method = operationType.ToString().ToUpperInvariant(),
            path,
            deprecated = true
        });

    public static string ForVersionedOperation(string path, OperationType operationType, string detectedVersion, string latestVersion) =>
        Serialize(new
        {
            method = operationType.ToString().ToUpperInvariant(),
            path,
            detectedVersion,
            latestVersion
        });

    private static object? BuildSchemaExcerpt(OpenApiSchema? schema)
    {
        if (schema is null)
        {
            return null;
        }

        return new
        {
            type = schema.Type,
            format = string.IsNullOrWhiteSpace(schema.Format) ? null : schema.Format,
            readOnly = schema.ReadOnly == true ? (bool?)true : null,
            writeOnly = schema.WriteOnly == true ? (bool?)true : null,
            minLength = schema.MinLength,
            maxLength = schema.MaxLength,
            pattern = string.IsNullOrWhiteSpace(schema.Pattern) ? null : schema.Pattern,
            minimum = schema.Minimum?.ToString(),
            maximum = schema.Maximum?.ToString(),
            enumCount = schema.Enum.Count > 0 ? (int?)schema.Enum.Count : null,
            propertyCount = schema.Properties.Count > 0 ? (int?)schema.Properties.Count : null,
            required = schema.Required.Count > 0 ? schema.Required.OrderBy(x => x, StringComparer.Ordinal).ToArray() : null,
            itemsType = schema.Items?.Type
        };
    }

    private static string Serialize(object value) => JsonSerializer.Serialize(value, SerializerOptions);
}
