// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Refuses inline lowering when construction or enclosing control flow has semantics the recovered production loses.
/// </summary>
public static class InlineProductionShape
{
    /// <summary>
    /// Determines whether a body is free of branching, repetition and deferred or exceptional execution.
    /// </summary>
    /// <param name="body">The body to inspect, including an aggregate behavior's caller.</param>
    /// <returns>Whether unconditional production is provable from this body.</returns>
    public static bool IsUnconditional(SyntaxNode body) =>
        !body.DescendantNodesAndSelf().Any(node => node is
            IfStatementSyntax or ConditionalExpressionSyntax or ConditionalAccessExpressionSyntax or SwitchStatementSyntax or SwitchExpressionSyntax or
            ForStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax or WhileStatementSyntax or
            DoStatementSyntax or TryStatementSyntax or GotoStatementSyntax or AnonymousFunctionExpressionSyntax or
            LocalFunctionStatementSyntax or YieldStatementSyntax or BinaryExpressionSyntax or ThrowStatementSyntax or ThrowExpressionSyntax);

    /// <summary>
    /// Determines whether the constructor is a positional record constructor or implicit parameterless construction.
    /// </summary>
    /// <param name="creation">The construction to inspect.</param>
    /// <param name="model">The semantic model owning the construction.</param>
    /// <returns>Whether mappings describe the constructor without guessing at imperative constructor code.</returns>
    public static bool IsSupported(BaseObjectCreationExpressionSyntax creation, SemanticModel model) =>
        model.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor &&
        (constructor.DeclaringSyntaxReferences.Any(_ => _.GetSyntax() is RecordDeclarationSyntax) ||
         (constructor.IsImplicitlyDeclared && constructor.Parameters.Length == 0));
}
