// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Constraints;

/// <summary>
/// Resolves the name a constraint rule is declared under.
/// </summary>
/// <remarks>
/// A rule can be named outright, named by a literal handed to it, or not named at all. The last case falls back to
/// a name derived from what the rule constrains, because a constraint nothing can refer to is of no use to a reader.
/// </remarks>
public static class ConstraintNames
{
    /// <summary>
    /// The call naming a uniqueness rule.
    /// </summary>
    public const string WithName = "WithName";

    /// <summary>
    /// Resolves the name a rule is declared under, falling back to a name derived from what it constrains.
    /// </summary>
    /// <param name="invocation">The call declaring the rule.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="type">The type declaring the constraint.</param>
    /// <param name="eventType">The event type being constrained.</param>
    /// <param name="property">The property being constrained, if there is one.</param>
    /// <returns>The name.</returns>
    public static string Resolve(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        INamedTypeSymbol type,
        ITypeSymbol eventType,
        string? property = null)
    {
        var named = invocation.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(_ => string.Equals(InvocationChain.NameOf(_), WithName, StringComparison.Ordinal));

        if (named is not null && InvocationChain.ArgumentOf(named) is { } argument &&
            semanticModel.GetConstantValue(argument).Value is string declared && declared.Length > 0)
        {
            return declared;
        }

        var literal = ConstantArgument(invocation, semanticModel);

        return literal ?? (property is null ? $"{type.Name}{eventType.Name}" : $"{type.Name}{eventType.Name}{property}");
    }

    /// <summary>
    /// Reads the name a generic uniqueness rule was given directly.
    /// </summary>
    /// <param name="invocation">The call declaring the rule.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <returns>The name, or <see langword="null"/> when none was given.</returns>
    static string? ConstantArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel) =>
        invocation.ArgumentList.Arguments
            .Select(_ => semanticModel.GetConstantValue(_.Expression).Value as string)
            .FirstOrDefault(_ => !string.IsNullOrEmpty(_));
}
