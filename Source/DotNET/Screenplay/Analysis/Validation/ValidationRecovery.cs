// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Distinguishes an empty default validator from validation whose executable rules were lost.
/// </summary>
internal static class ValidationRecovery
{
    /// <summary>
    /// Checks that every default constructor registration was recovered without weakening it.
    /// </summary>
    /// <param name="validator">The validator declaring the registrations.</param>
    /// <param name="models">The source semantic models.</param>
    /// <returns>Whether the default validation contract is complete.</returns>
    internal static bool IsComplete(INamedTypeSymbol validator, SemanticModels models)
    {
        if (validator.DeclaringSyntaxReferences.Length == 0 || validator.BaseType is not { } parent ||
            (!parent.Is(WellKnownTypeNames.AbstractValidator) && !parent.Is("Cratis.Arc.Validation.ConceptValidator`1")))
        {
            return false;
        }

        return validator.InstanceConstructors.All(constructor => constructor.IsImplicitlyDeclared ||
            constructor.DeclaringSyntaxReferences.All(reference => reference.GetSyntax() is ConstructorDeclarationSyntax declaration &&
                declaration.Initializer is null && models.For(declaration.SyntaxTree) is { } model &&
                (declaration.Body is { } body ? Statements(body, model) : declaration.ExpressionBody is { } expression && Registration(expression.Expression, model))));
    }

    static bool Statements(BlockSyntax block, SemanticModel model) => block.Statements.All(statement =>
        statement is ExpressionStatementSyntax expression && Registration(expression.Expression, model));

    static bool Registration(ExpressionSyntax expression, SemanticModel model)
    {
        if (expression is InvocationExpressionSyntax invocation && InvocationChain.Unwind(invocation)?.RootName == "RuleSet")
        {
            var argument = invocation.ArgumentList.Arguments.FirstOrDefault();
            if (argument is null || model.GetConstantValue(argument.Expression) is not { HasValue: true, Value: string name })
            {
                return false;
            }

            // Named, non-default rule sets do not run in the default concept validation contract.
            return !name.Split(',', ';').Any(part => part.Trim().Equals("default", StringComparison.OrdinalIgnoreCase)) ||
                (invocation.ArgumentList.Arguments.LastOrDefault()?.Expression is LambdaExpressionSyntax { Body: BlockSyntax body } && Statements(body, model));
        }
        if (InvocationChain.Unwind(expression) is not { RootName: ValidationReader.RuleFor } chain)
        {
            return false;
        }
        var diagnostics = new ScreenplayDiagnostics();
        var rules = new List<ValidationRuleModel>();
        new ValidationChainReader(diagnostics).Read(chain, false, model, string.Empty, rules);

        return diagnostics.All.Count == 0 && rules.TrueForAll(rule => rule.Kind != ValidationRuleKind.Rule &&
            rule.Property == "Value" && (rule.Kind == ValidationRuleKind.NotEmpty || rule.Value is not null));
    }
}
