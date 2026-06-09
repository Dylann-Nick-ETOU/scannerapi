using ApiSecurityScanner.Application.Rules;
using FluentAssertions;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Tests.Rules;

public class MassAssignmentRuleTests
{
    [Fact]
    public void Evaluate_ShouldDetectWritableSensitiveFieldInRequestBody()
    {
        var rule = new MassAssignmentRule();
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/users"] = new OpenApiPathItem
                {
                    Operations =
                    {
                        [OperationType.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content =
                                {
                                    ["application/json"] = new OpenApiMediaType
                                    {
                                        Schema = new OpenApiSchema
                                        {
                                            Type = "object",
                                            Properties =
                                            {
                                                ["email"] = new OpenApiSchema { Type = "string", Format = "email" },
                                                ["role"] = new OpenApiSchema { Type = "string" }
                                            }
                                        }
                                    }
                                }
                            },
                            Responses =
                            {
                                ["201"] = new OpenApiResponse { Description = "Created" }
                            }
                        }
                    }
                }
            }
        };

        var issues = rule.Evaluate(document);

        issues.Should().ContainSingle();
        issues[0].RuleCode.Should().Be("API-MASS-001");
        issues[0].Endpoint.Should().Be("Post /users");
        issues[0].DetectionConfidence.Should().Be("Medium");
        issues[0].Description.Should().Contain("role");
        issues[0].OpenApiLocation.Should().Be("/paths/~1users/post/requestBody/content/application~1json/schema/properties/role");
        issues[0].OpenApiExcerpt.Should().Contain("\"property\": \"role\"");
    }

    [Fact]
    public void Evaluate_ShouldIgnoreSensitiveFieldMarkedReadOnly()
    {
        var rule = new MassAssignmentRule();
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/users"] = new OpenApiPathItem
                {
                    Operations =
                    {
                        [OperationType.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content =
                                {
                                    ["application/json"] = new OpenApiMediaType
                                    {
                                        Schema = new OpenApiSchema
                                        {
                                            Type = "object",
                                            Properties =
                                            {
                                                ["role"] = new OpenApiSchema
                                                {
                                                    Type = "string",
                                                    ReadOnly = true
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            Responses =
                            {
                                ["201"] = new OpenApiResponse { Description = "Created" }
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
    public void Evaluate_ShouldDetectNestedSensitiveWritableField()
    {
        var rule = new MassAssignmentRule();
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/accounts"] = new OpenApiPathItem
                {
                    Operations =
                    {
                        [OperationType.Put] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content =
                                {
                                    ["application/json"] = new OpenApiMediaType
                                    {
                                        Schema = new OpenApiSchema
                                        {
                                            Type = "object",
                                            Properties =
                                            {
                                                ["profile"] = new OpenApiSchema
                                                {
                                                    Type = "object",
                                                    Properties =
                                                    {
                                                        ["permissions"] = new OpenApiSchema
                                                        {
                                                            Type = "array",
                                                            Items = new OpenApiSchema { Type = "string" }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            Responses =
                            {
                                ["200"] = new OpenApiResponse { Description = "Updated" }
                            }
                        }
                    }
                }
            }
        };

        var issues = rule.Evaluate(document);

        issues.Should().ContainSingle();
        issues[0].Description.Should().Contain("profile.permissions");
        issues[0].OpenApiLocation.Should().Be("/paths/~1accounts/put/requestBody/content/application~1json/schema/properties/profile/properties/permissions");
        issues[0].OpenApiExcerpt.Should().Contain("\"property\": \"profile.permissions\"");
    }
}
