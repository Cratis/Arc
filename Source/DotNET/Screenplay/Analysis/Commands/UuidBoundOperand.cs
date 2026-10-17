// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Retains each call-site expression and its owning model so binding cannot hide a refused operand.
/// </summary>
/// <param name="Expressions">The expressions from the behavior parameter through to its supplied value.</param>
internal sealed record UuidBoundOperand(IReadOnlyList<BoundArgument> Expressions)
{
    /// <summary>
    /// Gets the supplied value and the model that owns it.
    /// </summary>
    internal BoundArgument Value => Expressions[^1];

    /// <summary>
    /// Gets the supplied operand's type, before any wrapper is stripped for path or literal recovery.
    /// </summary>
    internal ITypeSymbol? Type => Value.SemanticModel.GetTypeInfo(Value.Expression).Type;

    /// <summary>
    /// Gets whether any expression in the binding chain changes type through an explicit cast.
    /// </summary>
    internal bool HasNonTransparentCast => Expressions.Any(argument => argument.Expression.DescendantNodesAndSelf()
        .OfType<CastExpressionSyntax>()
        .Any(cast => !SymbolEqualityComparer.IncludeNullability.Equals(argument.SemanticModel.GetTypeInfo(cast.Expression).Type, argument.SemanticModel.GetTypeInfo(cast).Type)));

    /// <summary>
    /// Gets whether a binding or its unwrapped input can be absent.
    /// </summary>
    internal bool IsOptional => Expressions.Any(argument =>
        UuidComparisonOperands.IsOptional(argument.SemanticModel.GetTypeInfo(argument.Expression).Type) ||
        UuidComparisonOperands.IsOptional(argument.SemanticModel.GetTypeInfo(MappingSourceReader.Unwrap(argument.Expression)).Type));

    /// <summary>
    /// Follows parameter bindings without losing wrappers or crossing semantic models incorrectly.
    /// </summary>
    /// <param name="expression">The expression in the comparison.</param>
    /// <param name="model">Its semantic model.</param>
    /// <param name="bindings">The call-site parameter bindings.</param>
    /// <returns>The resolved operand, or null for a cycle or an overlong chain.</returns>
    internal static UuidBoundOperand? Resolve(ExpressionSyntax expression, SemanticModel model, ParameterBindings? bindings)
    {
        var expressions = new List<BoundArgument>();
        var visited = new HashSet<ExpressionSyntax>();
        var current = new BoundArgument(expression, model);
        while (expressions.Count < 64 && visited.Add(current.Expression))
        {
            expressions.Add(current);
            if (bindings?.Resolve(MappingSourceReader.Unwrap(current.Expression), current.SemanticModel) is not { } next)
            {
                return new(expressions);
            }

            current = next;
        }

        return null;
    }
}
