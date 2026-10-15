// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Follows local value origins conservatively without treating reassignment as a new caller input.
/// </summary>
internal static class LocalValueOrigins
{
    /// <summary>
    /// Collects a local's initializer and assignments by symbol identity.
    /// </summary>
    /// <param name="local">The local being followed.</param>
    /// <param name="model">The model resolving its declarations and assignments.</param>
    /// <returns>Every expression that may supply the local's value.</returns>
    internal static IEnumerable<ExpressionSyntax> Values(ILocalSymbol local, SemanticModel model)
    {
        foreach (var declaration in local.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()))
        {
            if (declaration is VariableDeclaratorSyntax { Initializer.Value: { } value })
            {
                yield return value;
            }

            var scope = declaration.Ancestors().FirstOrDefault(node => node is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax);
            var assignments = scope?.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Where(assignment => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(assignment.Left).Symbol, local)) ?? [];
            foreach (var assignment in assignments)
            {
                yield return assignment.Right;
            }
        }
    }

    /// <summary>
    /// Proves that an expression can carry a particular event construction through local aliases or assignments.
    /// </summary>
    /// <param name="expression">The use of the value.</param>
    /// <param name="creation">The event construction being followed.</param>
    /// <param name="model">The model resolving local symbols.</param>
    /// <returns>Whether the expression can contain the construction.</returns>
    internal static bool Reaches(ExpressionSyntax expression, SyntaxNode creation, SemanticModel model) => Reaches(expression, creation, model, new(SymbolEqualityComparer.Default));

    static bool Reaches(ExpressionSyntax expression, SyntaxNode creation, SemanticModel model, HashSet<ISymbol> visited)
    {
        if (expression.SyntaxTree == creation.SyntaxTree && expression.Span.Contains(creation.Span))
        {
            return true;
        }

        return expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(identifier =>
            model.GetSymbolInfo(identifier).Symbol is ILocalSymbol local && visited.Add(local) &&
            Values(local, model).Any(value => Reaches(value, creation, model, visited)));
    }
}
