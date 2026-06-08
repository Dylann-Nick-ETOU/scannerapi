using ApiSecurityScanner.Application.Rules;
using FluentAssertions;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Tests.Rules;

public class WeakInputValidationRuleTests
{
    [Fact]
    public void Evaluate_ShouldDetectWeakQueryParameterWithoutConstraints()
    {
        var rule = new WeakInputValidationRule();
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/activities"] = new OpenApiPathItem
                {
                    Operations =
                    {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Parameters =
                            [
                                new OpenApiParameter
                                {
                                    Name = "search",
                                    In = ParameterLocation.Query,
                                    Required = false,
                                    Schema = new OpenApiSchema { Type = "string" }
                                }
                            ],
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
        issues[0].RuleCode.Should().Be("API-VALID-001");
        issues[0].Endpoint.Should().Be("Get /activities");
        issues[0].DetectionConfidence.Should().Be("Medium");
        issues[0].Description.Should().Contain("search");
        issues[0].OpenApiLocation.Should().Be("/paths/~1activities/get/parameters/0/schema");
        issues[0].OpenApiExcerpt.Should().Contain("\"name\": \"search\"");
    }

    [Fact]
    public void Evaluate_ShouldIgnoreConstrainedQueryParameter()
    {
        var rule = new WeakInputValidationRule();
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/activities"] = new OpenApiPathItem
                {
                    Operations =
                    {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Parameters =
                            [
                                new OpenApiParameter
                                {
                                    Name = "search",
                                    In = ParameterLocation.Query,
                                    Required = false,
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "string",
                                        MinLength = 3,
                                        MaxLength = 50
                                    }
                                }
                            ],
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

    [Fact]
    public void Evaluate_ShouldDetectPathParameterThatIsNotRequired()
    {
        var rule = new WeakInputValidationRule();
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/activities/{id}"] = new OpenApiPathItem
                {
                    Operations =
                    {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Parameters =
                            [
                                new OpenApiParameter
                                {
                                    Name = "id",
                                    In = ParameterLocation.Path,
                                    Required = false,
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Pattern = "^[0-9a-fA-F-]{36}$"
                                    }
                                }
                            ],
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
        issues[0].Title.Should().Contain("path");
        issues[0].Recommendation.Should().Contain("required");
        issues[0].OpenApiLocation.Should().Be("/paths/~1activities~1{id}/get/parameters/0");
        issues[0].OpenApiExcerpt.Should().Contain("\"required\": false");
    }
}
