using System.Net;
using System.Text;
using ApiSecurityScanner.Infrastructure.OpenApi;
using FluentAssertions;

namespace ApiSecurityScanner.Tests.Infrastructure;

public class OpenApiDocumentLoaderTests
{
    [Fact]
    public async Task LoadFromUrlAsync_ShouldRejectLoopbackAddress()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var loader = new OpenApiDocumentLoader(httpClient);

        var act = async () => await loader.LoadFromUrlAsync("http://127.0.0.1/openapi.json");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Loopback addresses are not allowed*");
    }

    [Fact]
    public async Task LoadFromUrlAsync_ShouldRejectPrivateIpv4Address()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var loader = new OpenApiDocumentLoader(httpClient);

        var act = async () => await loader.LoadFromUrlAsync("http://192.168.1.10/openapi.json");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Private IP addresses are not allowed*");
    }

    [Fact]
    public async Task LoadFromUrlAsync_ShouldRejectRedirectResponses()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri("https://example.com/openapi.json");
            return response;
        }));
        var loader = new OpenApiDocumentLoader(httpClient);

        var act = async () => await loader.LoadFromUrlAsync("https://example.com/source.json");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Redirect responses are not allowed*");
    }

    [Fact]
    public async Task LoadFromUrlAsync_ShouldParseValidPublicDocument()
    {
        const string openApi = """
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "v1" },
              "paths": {}
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(openApi, Encoding.UTF8, "application/json")
            }));
        var loader = new OpenApiDocumentLoader(httpClient);

        var document = await loader.LoadFromUrlAsync("https://example.com/openapi.json");

        document.Info.Title.Should().Be("Test");
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}
