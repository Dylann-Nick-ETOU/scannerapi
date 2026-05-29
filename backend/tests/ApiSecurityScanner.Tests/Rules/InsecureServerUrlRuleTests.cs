using ApiSecurityScanner.Application.Rules;
using FluentAssertions;
using Microsoft.OpenApi.Models;

namespace ApiSecurityScanner.Tests.Rules;

public class InsecureServerUrlRuleTests
{
    [Fact]
    public void Evaluate_ShouldDetectHttpServer()
    {
        var rule = new InsecureServerUrlRule();
        var document = new OpenApiDocument
        {
            Servers =
            [
                new OpenApiServer { Url = "http://api.example.local" }
            ]
        };

        var issues = rule.Evaluate(document);
        issues.Should().ContainSingle();
        issues[0].RuleCode.Should().Be("API-CONFIG-001");
    }
}
