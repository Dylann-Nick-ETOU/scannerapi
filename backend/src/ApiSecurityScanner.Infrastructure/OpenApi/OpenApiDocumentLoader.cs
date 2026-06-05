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
