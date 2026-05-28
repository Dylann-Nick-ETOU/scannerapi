using ApiSecurityScanner.Application;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace ApiSecurityScanner.Infrastructure.OpenApi;

public class OpenApiDocumentLoader(HttpClient httpClient) : IOpenApiDocumentLoader
{
    public async Task<OpenApiDocument> LoadFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return Parse(content);
    }

    public OpenApiDocument LoadFromText(string content)
    {
        return Parse(content);
    }

    private static OpenApiDocument Parse(string content)
    {
        var reader = new OpenApiStringReader();
        var document = reader.Read(content, out var diagnostic);

        if (diagnostic.Errors.Count > 0)
        {
            var details = string.Join("; ", diagnostic.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"OpenAPI parsing failed: {details}");
        }

        return document;
    }
}
