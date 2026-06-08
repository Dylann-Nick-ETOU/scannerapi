using ApiSecurityScanner.Application.Rules;
using FluentAssertions;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Tests.Rules;

public class InventoryManagementRuleTests
{
    [Fact]
    public void Evaluate_ShouldDetectDeprecatedEndpoint()
    {
        var rule = new InventoryManagementRule();
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/v1/users"] = new OpenApiPathItem
                {
                    Operations =
                    {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Deprecated = true,
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

        issues.Should().ContainSingle();
        issues[0].Title.Should().Contain("obsolète");
        issues[0].Endpoint.Should().Be("Get /v1/users");
    }

    [Fact]
    public void Evaluate_ShouldDetectOlderMajorVersionWhenMultipleVersionsCoexist()
    {
        var rule = new InventoryManagementRule();
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/v1/users"] = new OpenApiPathItem
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
                },
                ["/v2/users"] = new OpenApiPathItem
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

        issues.Should().ContainSingle();
        issues[0].Title.Should().Contain("Ancienne version");
        issues[0].Endpoint.Should().Be("Get /v1/users");
        issues[0].Description.Should().Contain("v2");
    }

    [Fact]
    public void Evaluate_ShouldIgnoreSingleVersionApi()
    {
        var rule = new InventoryManagementRule();
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/v1/users"] = new OpenApiPathItem
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

        issues.Should().BeEmpty();
    }
}
