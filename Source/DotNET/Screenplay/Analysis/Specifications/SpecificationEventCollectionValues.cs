// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads exact ordered collection expectations from direct event-property predicates.
/// </summary>
static class SpecificationEventCollectionValues
{
    static readonly MappingSourceReader _sources = new(new());

    /// <summary>
    /// Reads an Enumerable sequence equality or a negated parameterless Any call.
    /// </summary>
    /// <param name="expression">The predicate conjunct.</param>
    /// <param name="eventType">The expected event type.</param>
    /// <param name="parameter">The predicate's event parameter.</param>
    /// <param name="model">The semantic model resolving the predicate.</param>
    /// <param name="reason">Why an applied element conversion cannot be stated faithfully.</param>
    /// <param name="models">The models of every analyzed project, or null for a single-compilation reading.</param>
    /// <returns>The complete collection expectation, or null for any other predicate.</returns>
    internal static PropertyMappingModel? Read(ExpressionSyntax expression, ITypeSymbol eventType, IParameterSymbol parameter, SemanticModel model, out string? reason, SemanticModels? models = null)
    {
        reason = null;
        var empty = expression is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression };
        var call = empty ? Unwrap(((PrefixUnaryExpressionSyntax)expression).Operand) : expression;
        if (call is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation ||
            PropertyOf(Unwrap(member.Expression), eventType, parameter, model) is not { } property ||
            CollectionElements.ElementOf(property.Type) is not { } elementType ||
            !IsEnumerableCall(invocation, model, elementType, empty ? "Any" : "SequenceEqual", empty ? 0 : 1))
        {
            return null;
        }

        var literal = empty
            ? new LiteralSource(Array.Empty<object?>())
            : SpecificationCollections.Read(invocation.ArgumentList.Arguments[0].Expression, model, property.Type, _sources, out reason, models);

        return literal is null ? null : new(property.Name, literal);
    }

    static IPropertySymbol? PropertyOf(ExpressionSyntax expression, ITypeSymbol eventType, IParameterSymbol parameter, SemanticModel model) =>
        expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } member &&
        SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, parameter) &&
        model.GetSymbolInfo(member).Symbol is IPropertySymbol property &&
        SymbolEqualityComparer.Default.Equals(property.ContainingType, eventType)
            ? property
            : null;

    static bool IsEnumerableCall(InvocationExpressionSyntax invocation, SemanticModel model, ITypeSymbol elementType, string name, int arguments) =>
        invocation.ArgumentList.Arguments.Count == arguments &&
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ReducedFrom: { } definition, TypeArguments: [var comparedType] } &&
        SymbolEqualityComparer.Default.Equals(comparedType, elementType) &&
        definition.ContainingType.FullMetadataName() == "System.Linq.Enumerable" &&
        string.Equals(definition.Name, name, StringComparison.Ordinal) &&
        definition.Parameters.Length == arguments + 1;

    static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }
}
