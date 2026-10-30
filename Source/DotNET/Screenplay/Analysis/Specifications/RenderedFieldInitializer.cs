// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Proves a renderer-owned field has only its declaration initializer, including readonly constructor writes.
/// </summary>
internal static class RenderedFieldInitializer
{
    /// <summary>Reads one initializer without following aliases or accepting inherited fields.</summary>
    /// <param name="field">The exact field being used.</param>
    /// <param name="fixture">The descriptor's concrete fixture.</param>
    /// <param name="compilation">The compilation containing every possible source write.</param>
    /// <returns>The initializer, or null when there is another write.</returns>
    internal static ExpressionSyntax? Read(IFieldSymbol field, INamedTypeSymbol fixture, Compilation compilation)
    {
        if (!field.IsReadOnly || field.IsConst ||
            !SymbolEqualityComparer.Default.Equals(field.ContainingType, fixture) ||
            field.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).ToList() is not
                [VariableDeclaratorSyntax { Initializer.Value: var initializer }])
        {
            return null;
        }

        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            if (tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
                .Any(expression => expression.Identifier.ValueText == field.Name &&
                    SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(expression).Symbol, field) && IsWritten(expression)))
            {
                return null;
            }
        }

        return initializer;
    }

    static bool IsWritten(ExpressionSyntax reference)
    {
        SyntaxNode use = reference.Parent is MemberAccessExpressionSyntax member && member.Name == reference ? member : reference;
        return use.Ancestors().Any(ancestor => ancestor switch
        {
            AssignmentExpressionSyntax assignment => assignment.Left.Span.Contains(use.Span),
            ArgumentSyntax argument => !argument.RefKindKeyword.IsKind(SyntaxKind.None),
            RefExpressionSyntax => true,
            PrefixUnaryExpressionSyntax prefix => prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression) || prefix.IsKind(SyntaxKind.AddressOfExpression),
            PostfixUnaryExpressionSyntax postfix => postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression),
            _ => false
        });
    }
}
