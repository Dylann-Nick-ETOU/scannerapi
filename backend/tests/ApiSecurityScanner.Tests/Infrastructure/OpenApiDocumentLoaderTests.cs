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

    [Fact]
    public async Task LoadFromUrlAsync_ShouldExplainSwaggerUiHtmlAndSuggestSpecUrls()
    {
        const string html = """
            <!DOCTYPE html>
            <html>
            <head><title>Swagger UI</title></head>
            <body>swagger-ui</body>
            </html>
            """;

        const string indexJs = """
            window.onload = function () {
              var configObject = JSON.parse('{"urls":[{"url":"/swagger/docs/v1/lyco","name":"Lyco API - v1"},{"url":"/swagger/docs/v1/company","name":"Company API - v1"}]}');
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsoluteUri == "https://example.com/swagger/index.html")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(html, Encoding.UTF8, "text/html")
                };
            }

            if (request.RequestUri.AbsoluteUri == "https://example.com/swagger/index.js")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(indexJs, Encoding.UTF8, "application/javascript")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));
        var loader = new OpenApiDocumentLoader(httpClient);

        var act = async () => await loader.LoadFromUrlAsync("https://example.com/swagger/index.html");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*points to Swagger UI HTML*")
            .WithMessage("*https://example.com/swagger/docs/v1/lyco*")
            .WithMessage("*https://example.com/swagger/docs/v1/company*");
    }

    [Fact]
    public void LoadFromText_ShouldNormalizeInvalidSchemaKeys()
    {
        const string openApi = """
            {
              "openapi": "3.0.4",
              "info": { "title": "Test", "version": "v1" },
              "paths": {
                "/items": {
                  "get": {
                    "responses": {
                      "200": {
                        "description": "OK",
                        "content": {
                          "application/json": {
                            "schema": {
                              "$ref": "#/components/schemas/Type`1[[Foo]]"
                            }
                          }
                        }
                      }
                    }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Type`1[[Foo]]": {
                    "type": "object",
                    "properties": {
                      "id": { "type": "integer", "format": "int32" }
                    }
                  }
                }
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var loader = new OpenApiDocumentLoader(httpClient);

        var document = loader.LoadFromText(openApi);

        document.Components.Schemas.Keys.Should().ContainSingle();
        document.Components.Schemas.Keys.Single().Should().Be("Type_1_Foo");
        document.Paths["/items"].Operations[Microsoft.OpenApi.Models.OperationType.Get]
            .Responses["200"].Content["application/json"].Schema.Reference!.Id
            .Should().Be("Type_1_Foo");
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}
