// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Proves that wrapper metadata is already represented by payload mappings or event tags.
/// </summary>
internal static class ReactionEnvelopeInitializers
{
    /// <summary>
    /// Accepts only initializer values that lose no metadata when the wrapper is removed.
    /// </summary>
    /// <param name="envelope">The semantically bound wrapper and payload.</param>
    /// <param name="model">The model resolving initializer members.</param>
    /// <param name="handler">The handler supplying trigger and context values.</param>
    /// <returns>Whether every initialized member is already represented.</returns>
    internal static bool IsRepresented(ExplicitEventDestination envelope, SemanticModel model, IMethodSymbol handler)
    {
        if (envelope.Wrapper.Initializer is not { } initializer)
        {
            return true;
        }

        var assigned = new HashSet<string>(StringComparer.Ordinal);
        return initializer.Expressions.Count > 0 && initializer.Expressions.All(expression =>
            expression is AssignmentExpressionSyntax { Left: IdentifierNameSyntax name } assignment &&
            assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) &&
            assigned.Add(name.Identifier.ValueText) &&
            IsRepresented(assignment, envelope, model, handler));
    }

    static bool IsRepresented(AssignmentExpressionSyntax assignment, ExplicitEventDestination envelope, SemanticModel model, IMethodSymbol handler) =>
        model.GetSymbolInfo(assignment.Left).Symbol is IPropertySymbol { IsStatic: false } property &&
        SymbolEqualityComparer.Default.Equals(property.ContainingType, envelope.WrapperType) &&
        property.Name switch
        {
            ReactionReader.OccurredProperty => OccurrenceIsRepresented(assignment.Right, envelope, model, handler),
            "Tags" => TagsAreRepresented(assignment.Right, envelope.EventType),
            _ => false
        };

    static bool OccurrenceIsRepresented(ExpressionSyntax value, ExplicitEventDestination envelope, SemanticModel model, IMethodSymbol handler)
    {
        var context = handler.Parameters.ElementAtOrDefault(1);
        if (context?.Type.Is(WellKnownTypeNames.EventContext) != true || !IsContextOccurrence(value, model, context))
        {
            return false;
        }

        return ReactionMappings.Read(envelope.Event, model, envelope.EventType, handler.Parameters[0], context) is { } mappings &&
            mappings.Any(mapping => mapping.Source is ContextSource source && string.Equals(source.Path, ReactionReader.OccurredContext, StringComparison.Ordinal));
    }

    static bool IsContextOccurrence(ExpressionSyntax value, SemanticModel model, IParameterSymbol context) =>
        value is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } member &&
        model.GetSymbolInfo(member).Symbol is IPropertySymbol { IsStatic: false } property &&
        string.Equals(property.Name, ReactionReader.OccurredProperty, StringComparison.Ordinal) &&
        SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, context);

    static bool TagsAreRepresented(ExpressionSyntax value, INamedTypeSymbol eventType) =>
        value is CollectionExpressionSyntax collection &&
        collection.Elements.All(element => element is ExpressionElementSyntax { Expression: LiteralExpressionSyntax literal } && literal.IsKind(SyntaxKind.StringLiteralExpression)) &&
        collection.Elements.Cast<ExpressionElementSyntax>().Select(element => ((LiteralExpressionSyntax)element.Expression).Token.ValueText)
            .SequenceEqual(Tags.Of(eventType), StringComparer.Ordinal);
}
