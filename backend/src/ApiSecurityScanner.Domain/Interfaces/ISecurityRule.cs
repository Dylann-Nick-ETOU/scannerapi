using ApiSecurityScanner.Domain.Entities;

namespace ApiSecurityScanner.Domain.Interfaces;

public interface ISecurityRule
{
    string RuleCode { get; }
    string Name { get; }
    IReadOnlyList<SecurityIssue> Evaluate(object document);
}
