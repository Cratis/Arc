// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Aggregates;

/// <summary>
/// Recognizes an expression that reads the state an aggregate root holds.
/// </summary>
/// <remarks>
/// A condition in a document compares the input of a command, and nothing else exists to compare. An aggregate root
/// deciding on what it has already seen is therefore a decision no <c>produces when</c> can carry - not because it
/// was read badly, but because the language has nowhere to put it. Telling that case apart from an expression that
/// simply could not be read is what makes the difference reportable.
/// </remarks>
public static class AggregateState
{
    /// <summary>
    /// Determines whether an expression reads the state of an aggregate root.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="aggregateRoot">The aggregate root declaring the body, if there is one.</param>
    /// <returns>True when the expression reads state the aggregate root holds.</returns>
    public static bool IsReadBy(ExpressionSyntax expression, SemanticModel semanticModel, INamedTypeSymbol? aggregateRoot) =>
        aggregateRoot is not null &&
        expression
            .DescendantNodesAndSelf()
            .OfType<SimpleNameSyntax>()
            .Any(_ => IsState(semanticModel.GetSymbolInfo(_).Symbol, aggregateRoot));

    /// <summary>
    /// Determines whether a symbol is state an aggregate root holds.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="aggregateRoot">The aggregate root to check against.</param>
    /// <returns>True when the symbol is state of the aggregate root.</returns>
    static bool IsState(ISymbol? symbol, INamedTypeSymbol aggregateRoot) =>
        symbol is IFieldSymbol or IPropertySymbol &&
        !symbol.IsStatic &&
        IsHeldBy(aggregateRoot, symbol.ContainingType);

    /// <summary>
    /// Determines whether an aggregate root holds the members a type declares, including the ones it inherits.
    /// </summary>
    /// <param name="aggregateRoot">The aggregate root to walk.</param>
    /// <param name="declaring">The type declaring the member.</param>
    /// <returns>True when the aggregate root holds them.</returns>
    static bool IsHeldBy(INamedTypeSymbol aggregateRoot, INamedTypeSymbol? declaring)
    {
        for (var current = aggregateRoot; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, declaring))
            {
                return true;
            }
        }

        return false;
    }
}
