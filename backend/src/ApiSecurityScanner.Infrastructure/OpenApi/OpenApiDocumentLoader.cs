using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ApiSecurityScanner.Application;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace ApiSecurityScanner.Infrastructure.OpenApi;

public class OpenApiDocumentLoader(HttpClient httpClient) : IOpenApiDocumentLoader
{
    public async Task<OpenApiDocument> LoadFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var candidates = BuildCandidateUrls(url);
        Exception? lastException = null;

        foreach (var candidate in candidates)
        {
            try
            {
                using var response = await httpClient.GetAsync(candidate, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return Parse(content);
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        throw new InvalidOperationException(
            $"Unable to download/parse OpenAPI document from '{url}'. Tried: {string.Join(", ", candidates)}",
            lastException);
    }

    public OpenApiDocument LoadFromText(string content)
    {
        return Parse(content);
    }

    private static List<string> BuildCandidateUrls(string rawUrl)
    {
        var candidates = new List<string> { rawUrl };

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            return candidates;
        }

        var isLoopback = string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host == "127.0.0.1";

        if (!isLoopback)
        {
            return candidates;
        }

        var inContainer = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!inContainer)
        {
            return candidates;
        }

        var hostGateway = new UriBuilder(uri) { Host = "host.docker.internal" }.Uri.ToString();
        if (!candidates.Contains(hostGateway, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(hostGateway);
        }

        if (uri.Port != 8080)
        {
            var internalUrl = new UriBuilder(uri) { Host = "localhost", Port = 8080 }.Uri.ToString();
            if (!candidates.Contains(internalUrl, StringComparer.OrdinalIgnoreCase))
            {
                candidates.Add(internalUrl);
            }
        }

        return candidates;
    }

    private static OpenApiDocument Parse(string content)
    {
        var normalized = NormalizeOpenApi(content);

        var reader = new OpenApiStringReader();
        var document = reader.Read(normalized, out var diagnostic);

        if (diagnostic.Errors.Count > 0)
        {
            var details = string.Join("; ", diagnostic.Errors.Select(e => e.Message));
            throw new InvalidOperationException(
                $"OpenAPI parsing failed. Ensure the document is valid and compatible (prefer OpenAPI 3.0.x). Details: {details}");
        }

        return document;
    }

    private static string NormalizeOpenApi(string content)
    {
        // Keep YAML or unknown payloads untouched.
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(content);
        }
        catch
        {
            return NormalizeOpenApiVersionString(content);
        }

        if (root is not JsonObject obj)
        {
            return NormalizeOpenApiVersionString(content);
        }

        var version = obj["openapi"]?.GetValue<string>() ?? string.Empty;
        if (!version.StartsWith("3.1", StringComparison.OrdinalIgnoreCase))
        {
            return content;
        }

        // Downgrade declared version for parser compatibility.
        obj["openapi"] = "3.0.3";

        NormalizeTypeArrays(obj);

        return obj.ToJsonString();
    }

    private static void NormalizeTypeArrays(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
            {
                if (obj.TryGetPropertyValue("type", out var typeNode) && typeNode is JsonArray typeArray)
                {
                    var values = typeArray
                        .Select(x => x?.GetValue<string>())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();

                    if (values.Count > 0)
                    {
                        var hasNull = values.Any(v => string.Equals(v, "null", StringComparison.OrdinalIgnoreCase));
                        var firstNonNull = values.FirstOrDefault(v => !string.Equals(v, "null", StringComparison.OrdinalIgnoreCase));

                        if (!string.IsNullOrWhiteSpace(firstNonNull))
                        {
                            obj["type"] = firstNonNull;
                            if (hasNull)
                            {
                                obj["nullable"] = true;
                            }
                        }
                        else
                        {
                            obj["type"] = "string";
                            obj["nullable"] = true;
                        }
                    }
                }

                foreach (var (_, child) in obj)
                {
                    NormalizeTypeArrays(child);
                }

                break;
            }
            case JsonArray arr:
            {
                foreach (var child in arr)
                {
                    NormalizeTypeArrays(child);
                }

                break;
            }
        }
    }

    private static string NormalizeOpenApiVersionString(string content)
    {
        var match = Regex.Match(content, "\"openapi\"\\s*:\\s*\"(?<version>[^\"]+)\"", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return content;
        }

        var version = match.Groups["version"].Value;
        if (!version.StartsWith("3.1", StringComparison.OrdinalIgnoreCase))
        {
            return content;
        }

        return Regex.Replace(
            content,
            "\"openapi\"\\s*:\\s*\"[^\"]+\"",
            "\"openapi\": \"3.0.3\"",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
    }
}
