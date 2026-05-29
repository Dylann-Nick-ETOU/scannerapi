using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Application;

public interface IOpenApiDocumentLoader
{
    Task<OpenApiDocument> LoadFromUrlAsync(string url, CancellationToken cancellationToken = default);
    OpenApiDocument LoadFromText(string content);
}
