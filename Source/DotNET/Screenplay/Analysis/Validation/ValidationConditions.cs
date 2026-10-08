// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Identifies constructor scopes that do not declare unconditional validation rules.
/// </summary>
internal static class ValidationConditions
{
    static readonly HashSet<string> _blocks = new(StringComparer.Ordinal)
    {
        "When", "Unless", "WhenAsync", "UnlessAsync", "Otherwise", "DependentRules"
    };

    /// <summary>
    /// Reads the scope preventing a chain from declaring unconditional rules.
    /// </summary>
    /// <param name="chain">The chain to read.</param>
    /// <param name="semanticModel">The model resolving rule-set names.</param>
    /// <returns>The conditional scope, or <see langword="null"/> for an unconditional scope.</returns>
    internal static string? ScopeOf(InvocationChain chain, SemanticModel semanticModel)
    {
        if (PrecedingExit(chain.Root) is { } exit)
        {
            return $"a preceding early exit '{exit}'";
        }

        foreach (var node in chain.Root.Ancestors())
        {
            if (node is IfStatementSyntax or SwitchStatementSyntax or ConditionalExpressionSyntax or
                ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax)
            {
                return $"an enclosing '{node.Kind()}'";
            }

            if (node is InvocationExpressionSyntax invocation &&
                invocation.ArgumentList.Arguments.Any(argument => argument.Expression is AnonymousFunctionExpressionSyntax lambda && lambda.Span.Contains(chain.Root.Span)) &&
                ConditionalBlock(invocation, semanticModel) is { } condition)
            {
                return condition;
            }
        }

        return null;
    }

    static SyntaxNode? PrecedingExit(SyntaxNode root)
    {
        for (var node = root; node is not (null or BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax); node = node.Parent)
        {
            if (node.Parent is BlockSyntax block && block.Statements.TakeWhile(statement => statement != node)
                .SelectMany(statement => statement.DescendantNodesAndSelf(descendIntoChildren: child =>
                    child is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)))
                .FirstOrDefault(statement => statement is ReturnStatementSyntax or ThrowStatementSyntax or ThrowExpressionSyntax or GotoStatementSyntax) is { } exit)
            {
                return exit;
            }
        }

        return null;
    }

    static string? ConditionalBlock(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var name = invocation.Expression is IdentifierNameSyntax identifier
            ? identifier.Identifier.ValueText : InvocationChain.NameOf(invocation);
        if (name == "RuleSet")
        {
            var argument = invocation.ArgumentList.Arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == "ruleSetName") ??
                invocation.ArgumentList.Arguments.FirstOrDefault(argument => argument.NameColon is null);
            var value = argument is null ? default : semanticModel.GetConstantValue(argument.Expression);

            return value is { HasValue: true, Value: string ruleSet } && string.Equals(ruleSet, "default", StringComparison.OrdinalIgnoreCase)
                ? null : $"RuleSet({argument?.Expression})";
        }

        return _blocks.Contains(name) ? $"the enclosing '{name}' block" : null;
    }
}
