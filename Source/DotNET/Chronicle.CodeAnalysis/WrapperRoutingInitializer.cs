// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Reads only literal routing assignments on an event wrapper initializer.
/// </summary>
/// <param name="initializer">The wrapper initializer.</param>
/// <param name="model">The initializer's semantic model.</param>
internal sealed class WrapperRoutingInitializer(InitializerExpressionSyntax initializer, SemanticModel model)
{
    /// <summary>
    /// Gets an explicitly assigned routing value.
    /// </summary>
    /// <param name="property">The routing property.</param>
    /// <returns>The assigned expression, if any.</returns>
    internal ExpressionSyntax? ValueOf(string property) => initializer.Expressions.OfType<AssignmentExpressionSyntax>()
        .FirstOrDefault(assignment => assignment.Left is IdentifierNameSyntax name && name.Identifier.ValueText == property)?.Right;

    /// <summary>
    /// Reads a literal string, treating absent values and named or literal sentinels as unset.
    /// </summary>
    /// <param name="property">The routing property.</param>
    /// <param name="value">The literal value, or null for an unset property.</param>
    /// <returns>False for computed values whose route cannot be known.</returns>
    internal bool TryReadString(string property, out string? value)
    {
        var expression = ValueOf(property);
        value = null;
        if (expression?.IsKind(SyntaxKind.NullLiteralExpression) != false)
        {
            return true;
        }

        var sentinel = SentinelFor(property);
        if (IsSentinel(expression, sentinel))
        {
            return true;
        }

        var literal = Unwrap(expression);
        if (literal is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } text)
        {
            return false;
        }

        value = text.Token.ValueText == sentinel ? null : text.Token.ValueText;

        return true;
    }

    /// <summary>
    /// Resolves an explicitly assigned typeof definition.
    /// </summary>
    /// <returns>The definition symbol, if explicitly known.</returns>
    internal INamedTypeSymbol? TypedDefinition() => ValueOf("EventSource") is TypeOfExpressionSyntax typeOf
        ? model.GetTypeInfo(typeOf.Type).Type as INamedTypeSymbol
        : null;

    /// <summary>
    /// Reads the enclosing model-bound command's definition declaration.
    /// </summary>
    /// <returns>The declaration, or null outside a definition-routed command.</returns>
    internal AttributeData? CommandDeclaration()
    {
        var type = model.GetEnclosingSymbol(initializer.SpanStart)?.ContainingType;
        if (type is null || EventSourceDefinitionSymbols.Find(type, "Cratis.Arc.Commands.ModelBound.CommandAttribute") is null)
        {
            return null;
        }

        return EventSourceDefinitionSymbols.Find(type, EventSourceDefinitionSymbols.DeclarationAttribute);
    }

    /// <summary>
    /// Determines whether a with expression inherits routing the analyzer cannot establish.
    /// </summary>
    /// <returns>True when inheritance would require data-flow analysis.</returns>
    internal bool HasUnknownInheritedRouting()
    {
        if (initializer.Parent is not WithExpressionSyntax withExpression || TypedDefinition() is not null)
        {
            return false;
        }

        return withExpression.Expression is not BaseObjectCreationExpressionSyntax { Initializer: null };
    }

    static string? SentinelFor(string property) => property switch
    {
        "EventSourceType" => "Default",
        "EventStreamType" => "All",
        _ => null
    };

    bool IsSentinel(ExpressionSyntax expression, string? sentinel) => sentinel is not null &&
        model.GetSymbolInfo(expression).Symbol is IFieldSymbol { IsStatic: true } field &&
        field.Name == sentinel && EventSourceDefinitionSymbols.FullName(field.ContainingType) ==
            $"Cratis.Chronicle.Events.{(sentinel == "Default" ? "EventSourceType" : "EventStreamType")}";

    ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        if (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            return Unwrap(parenthesized.Expression);
        }

        if (expression is CastExpressionSyntax cast && IsTransparentCast(cast))
        {
            return Unwrap(cast.Expression);
        }

        return expression is BaseObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 1 } creation && IsRoutingConcept(creation)
            ? Unwrap(creation.ArgumentList.Arguments[0].Expression)
            : expression;
    }

    /// <summary>
    /// Determines whether a cast keeps the routing value it converts unchanged.
    /// </summary>
    /// <param name="cast">The cast to inspect.</param>
    /// <returns>True when the cast is to string or to a routing concept.</returns>
    bool IsTransparentCast(CastExpressionSyntax cast) =>
        model.GetTypeInfo(cast.Type).Type?.SpecialType == SpecialType.System_String || IsRoutingConcept(cast);

    bool IsRoutingConcept(ExpressionSyntax expression)
    {
        var name = EventSourceDefinitionSymbols.FullName(model.GetTypeInfo(expression).Type as INamedTypeSymbol);

        return name == "Cratis.Chronicle.Events.EventSourceType" || name == "Cratis.Chronicle.Events.EventStreamType";
    }
}
