using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ApiSecurityScanner.Application;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using System.Net;
using System.Net.Sockets;

namespace ApiSecurityScanner.Infrastructure.OpenApi;

public class OpenApiDocumentLoader(HttpClient httpClient) : IOpenApiDocumentLoader
{
    private static readonly Regex ValidComponentKeyPattern = new("^[a-zA-Z0-9\\.\\-_]+$", RegexOptions.Compiled);

    public async Task<OpenApiDocument> LoadFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("The OpenAPI URL must be absolute.", nameof(url));
        }

        ValidateTargetUri(uri);

        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if ((int)response.StatusCode is >= 300 and < 400 || response.Headers.Location is not null)
        {
            throw new ArgumentException("Redirect responses are not allowed for OpenAPI URLs.", nameof(url));
        }

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        await EnsureNotSwaggerUiHtmlAsync(uri, response, content, cancellationToken);
        return Parse(content);
    }

    public OpenApiDocument LoadFromText(string content)
    {
        return Parse(content);
    }

    private static void ValidateTargetUri(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only HTTP and HTTPS URLs are allowed.");
        }

        if (!string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            throw new ArgumentException("Userinfo is not allowed in OpenAPI URLs.");
        }

        if (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Loopback addresses are not allowed for OpenAPI URLs.");
        }

        if (IPAddress.TryParse(uri.Host, out var ipAddress))
        {
            EnsurePublicIpAddress(ipAddress);
            return;
        }

        IPAddress[] addresses;
        try
        {
            addresses = Dns.GetHostAddresses(uri.Host);
        }
        catch (SocketException ex)
        {
            throw new ArgumentException($"Unable to resolve host '{uri.Host}'.", ex);
        }

        if (addresses.Length == 0)
        {
            throw new ArgumentException($"Unable to resolve host '{uri.Host}'.");
        }

        foreach (var address in addresses)
        {
            EnsurePublicIpAddress(address);
        }
    }

    private static void EnsurePublicIpAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            throw new ArgumentException("Loopback addresses are not allowed for OpenAPI URLs.");
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal || address.IsIPv6Teredo)
            {
                throw new ArgumentException("Private IPv6 addresses are not allowed for OpenAPI URLs.");
            }

            var bytes = address.GetAddressBytes();
            if ((bytes[0] & 0xFE) == 0xFC)
            {
                throw new ArgumentException("Unique local IPv6 addresses are not allowed for OpenAPI URLs.");
            }

            return;
        }

        var octets = address.GetAddressBytes();
        var first = octets[0];
        var second = octets[1];

        var isPrivate =
            first == 10 ||
            (first == 172 && second >= 16 && second <= 31) ||
            (first == 192 && second == 168) ||
            (first == 169 && second == 254) ||
            first == 127 ||
            first == 0;

        if (isPrivate)
        {
            throw new ArgumentException("Private IP addresses are not allowed for OpenAPI URLs.");
        }
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

    private async Task EnsureNotSwaggerUiHtmlAsync(
        Uri requestUri,
        HttpResponseMessage response,
        string content,
        CancellationToken cancellationToken)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        var isHtml =
            mediaType.Contains("text/html", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("<html", StringComparison.OrdinalIgnoreCase);

        if (!isHtml)
        {
            return;
        }

        var looksLikeSwaggerUi =
            content.Contains("Swagger UI", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("swagger-ui", StringComparison.OrdinalIgnoreCase);

        if (!looksLikeSwaggerUi)
        {
            throw new ArgumentException(
                $"The URL '{requestUri}' returns HTML, not an OpenAPI JSON/YAML document. Provide the direct OpenAPI specification URL instead.");
        }

        var suggestions = await TryGetSwaggerUiSuggestionsAsync(requestUri, cancellationToken);
        if (suggestions.Count == 0)
        {
            throw new ArgumentException(
                $"The URL '{requestUri}' points to Swagger UI HTML, not to an OpenAPI JSON/YAML document. Use the direct specification URL instead.");
        }

        throw new ArgumentException(
            $"The URL '{requestUri}' points to Swagger UI HTML, not to an OpenAPI JSON/YAML document. Try one of these specification URLs: {string.Join(", ", suggestions)}");
    }

    private async Task<List<string>> TryGetSwaggerUiSuggestionsAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        var indexJsUri = new Uri(requestUri, "index.js");
        try
        {
            using var response = await httpClient.GetAsync(indexJsUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var script = await response.Content.ReadAsStringAsync(cancellationToken);
            return ExtractSwaggerDocUrls(script, requestUri);
        }
        catch
        {
            return [];
        }
    }

    private static List<string> ExtractSwaggerDocUrls(string script, Uri requestUri)
    {
        var results = new List<string>();
        var matches = Regex.Matches(script, "\"url\"\\s*:\\s*\"(?<url>[^\"]+)\"", RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var raw = match.Groups["url"].Value;
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            Uri? resolved = null;
            if (raw.StartsWith("/", StringComparison.Ordinal))
            {
                resolved = new UriBuilder(requestUri.Scheme, requestUri.Host, requestUri.IsDefaultPort ? -1 : requestUri.Port, raw).Uri;
            }
            else if (Uri.TryCreate(raw, UriKind.Absolute, out var absolute))
            {
                resolved = absolute;
            }
            else
            {
                resolved = new Uri(requestUri, raw);
            }

            var value = resolved.ToString();
            if (!results.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                results.Add(value);
            }
        }

        return results;
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

        NormalizeComponentSchemaKeys(obj);

        var version = obj["openapi"]?.GetValue<string>() ?? string.Empty;
        if (!version.StartsWith("3.1", StringComparison.OrdinalIgnoreCase))
        {
            return obj.ToJsonString();
        }

        // Downgrade declared version for parser compatibility.
        obj["openapi"] = "3.0.3";

        NormalizeTypeArrays(obj);

        return obj.ToJsonString();
    }

    private static void NormalizeComponentSchemaKeys(JsonObject root)
    {
        if (root["components"] is not JsonObject components || components["schemas"] is not JsonObject schemas)
        {
            return;
        }

        var renames = new Dictionary<string, string>(StringComparer.Ordinal);
        var reserved = new HashSet<string>(schemas.Select(x => x.Key), StringComparer.Ordinal);

        foreach (var (key, _) in schemas)
        {
            if (ValidComponentKeyPattern.IsMatch(key))
            {
                continue;
            }

            var sanitized = SanitizeComponentKey(key);
            var unique = sanitized;
            var suffix = 1;

            while (reserved.Contains(unique))
            {
                unique = $"{sanitized}_{suffix++}";
            }

            reserved.Add(unique);
            renames[key] = unique;
        }

        if (renames.Count == 0)
        {
            return;
        }

        var normalizedSchemas = new JsonObject();
        foreach (var (key, value) in schemas)
        {
            var targetKey = renames.TryGetValue(key, out var renamed) ? renamed : key;
            normalizedSchemas[targetKey] = value?.DeepClone();
        }

        components["schemas"] = normalizedSchemas;
        RewriteSchemaReferences(root, renames);
    }

    private static string SanitizeComponentKey(string key)
    {
        var sanitized = Regex.Replace(key, "[^a-zA-Z0-9\\.\\-_]", "_");
        sanitized = Regex.Replace(sanitized, "_{2,}", "_").Trim('_');

        return string.IsNullOrWhiteSpace(sanitized) ? "Schema" : sanitized;
    }

    private static void RewriteSchemaReferences(JsonNode? node, IReadOnlyDictionary<string, string> renames)
    {
        switch (node)
        {
            case JsonObject obj:
            {
                if (obj["$ref"] is JsonValue refValue &&
                    refValue.TryGetValue<string>(out var reference) &&
                    reference.StartsWith("#/components/schemas/", StringComparison.Ordinal))
                {
                    var key = reference["#/components/schemas/".Length..];
                    if (renames.TryGetValue(key, out var renamed))
                    {
                        obj["$ref"] = $"#/components/schemas/{renamed}";
                    }
                }

                foreach (var (_, child) in obj)
                {
                    RewriteSchemaReferences(child, renames);
                }

                break;
            }
            case JsonArray arr:
            {
                foreach (var child in arr)
                {
                    RewriteSchemaReferences(child, renames);
                }

                break;
            }
        }
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
