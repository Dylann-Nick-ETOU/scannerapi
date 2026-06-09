using ApiSecurityScanner.Application.Rules;
using FluentAssertions;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Tests.Rules;

public class MissingAuthenticationRuleTests
{
    [Fact]
    public void Evaluate_ShouldDetectEndpointWithoutSecurity()
    {
        var rule = new MissingAuthenticationRule();
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/admin/users"] = new OpenApiPathItem
                {
                    Operations =
                    {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses =
                            {
                                ["200"] = new OpenApiResponse { Description = "OK" }
                            }
                        }
                    }
                }
            }
        };

        var issues = rule.Evaluate(document);

        issues.Should().HaveCount(1);
        issues[0].RuleCode.Should().Be("API-AUTH-001");
        issues[0].Endpoint.Should().Contain("/admin/users");
        issues[0].DetectionConfidence.Should().Be("High");
        issues[0].OpenApiLocation.Should().Be("/paths/~1admin~1users/get");
        issues[0].OpenApiExcerpt.Should().Contain("\"method\": \"GET\"");
        issues[0].OpenApiExcerpt.Should().Contain("\"path\": \"/admin/users\"");
        issues[0].OwaspTop10Id.Should().Be("API2");
        issues[0].OwaspTop10Version.Should().Be("2023");
        issues[0].OwaspTop10Title.Should().Be("Broken Authentication");
    }
}
