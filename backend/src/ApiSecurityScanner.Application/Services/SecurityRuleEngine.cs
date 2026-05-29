using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.Services;

public class SecurityRuleEngine(IEnumerable<ISecurityRule> rules)
{
    public IReadOnlyList<SecurityIssue> Analyze(object openApiDocument)
    {
        var issues = new List<SecurityIssue>();
        foreach (var rule in rules)
        {
            issues.AddRange(rule.Evaluate(openApiDocument));
        }

        return issues;
    }
}
