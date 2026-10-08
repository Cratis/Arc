// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Reads the values a reactor handler gives an event or command it constructs.
/// </summary>
/// <remarks>
/// A reaction maps values from its triggering event, the time it occurred and constants - nothing else. A construction
/// is only stated when every property it sets is one of those and every property that can be set is set, because a
/// property left at its default is a value the reaction would not state, and a value worked out by code is one it
/// cannot.
/// </remarks>
public static class ReactionMappings
{
    /// <summary>
    /// Reads every mapping of a construction.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="semanticModel">The model the construction is read through.</param>
    /// <param name="target">The type being constructed.</param>
    /// <param name="trigger">The parameter carrying the triggering event.</param>
    /// <param name="context">The parameter carrying the event context, if the handler takes it.</param>
    /// <returns>The mappings in the order the type declares its properties, or <see langword="null"/> when any value is code.</returns>
    public static IReadOnlyList<PropertyMappingModel>? Read(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel,
        INamedTypeSymbol target,
        IParameterSymbol trigger,
        IParameterSymbol? context)
    {
        var properties = target.DeclaredProperties().ToList();
        var constructor = semanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
        var mapped = new Dictionary<string, PropertyMappingModel>(StringComparer.Ordinal);
        var arguments = creation.ArgumentList?.Arguments ?? default;

        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            var name = argument.NameColon?.Name.Identifier.ValueText ??
                (constructor is not null && index < constructor.Parameters.Length ? constructor.Parameters[index].Name : null);
            var property = properties.Find(_ => string.Equals(_.Name, name, StringComparison.OrdinalIgnoreCase));
            if (property is null || !Add(mapped, property, argument.Expression, semanticModel, trigger, context))
            {
                return null;
            }
        }

        foreach (var initialized in creation.Initializer?.Expressions ?? [])
        {
            if (initialized is not AssignmentExpressionSyntax { Left: IdentifierNameSyntax } assignment ||
                semanticModel.GetSymbolInfo(assignment.Left).Symbol is not IPropertySymbol assigned ||
                properties.Find(_ => string.Equals(_.Name, assigned.Name, StringComparison.Ordinal)) is not { } property ||
                !Add(mapped, property, assignment.Right, semanticModel, trigger, context))
            {
                return null;
            }
        }

        if (properties.Exists(_ => _.SetMethod is not null && !mapped.ContainsKey(_.Name)))
        {
            return null;
        }

        return [.. properties.Where(_ => mapped.ContainsKey(_.Name)).Select(_ => mapped[_.Name])];
    }

    /// <summary>
    /// Adds the mapping of one property.
    /// </summary>
    /// <param name="mapped">The mappings read so far.</param>
    /// <param name="property">The property set.</param>
    /// <param name="expression">The expression setting it.</param>
    /// <param name="semanticModel">The model the expression is read through.</param>
    /// <param name="trigger">The parameter carrying the triggering event.</param>
    /// <param name="context">The parameter carrying the event context.</param>
    /// <returns>True when the value could be stated.</returns>
    static bool Add(
        Dictionary<string, PropertyMappingModel> mapped,
        IPropertySymbol property,
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        IParameterSymbol trigger,
        IParameterSymbol? context)
    {
        if (mapped.ContainsKey(property.Name) || SourceOf(expression, semanticModel, property.Type, trigger, context) is not { } source)
        {
            return false;
        }

        mapped[property.Name] = new(property.Name, source);

        return true;
    }

    /// <summary>
    /// Reads where one value comes from.
    /// </summary>
    /// <param name="expression">The expression giving the value.</param>
    /// <param name="semanticModel">The model the expression is read through.</param>
    /// <param name="target">The type of the property the value is given to.</param>
    /// <param name="trigger">The parameter carrying the triggering event.</param>
    /// <param name="context">The parameter carrying the event context.</param>
    /// <returns>The source, or <see langword="null"/> when the value is code.</returns>
    static MappingSourceModel? SourceOf(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol target,
        IParameterSymbol trigger,
        IParameterSymbol? context)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        var constant = semanticModel.GetConstantValue(expression);
        if (constant.HasValue)
        {
            return ConstantOf(expression, semanticModel, constant.Value);
        }

        if (expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } member ||
            semanticModel.GetSymbolInfo(member).Symbol is not IPropertySymbol { IsStatic: false } property ||
            !SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(member).Type, target))
        {
            return null;
        }

        var owner = semanticModel.GetSymbolInfo(receiver).Symbol;
        if (SymbolEqualityComparer.Default.Equals(owner, trigger) &&
            trigger.Type.DeclaredProperties().Any(_ => string.Equals(_.Name, property.Name, StringComparison.Ordinal)))
        {
            return new PropertyPathSource(property.Name);
        }

        return context is not null && SymbolEqualityComparer.Default.Equals(owner, context) &&
            string.Equals(property.Name, ReactionReader.OccurredProperty, StringComparison.Ordinal)
                ? new ContextSource(ReactionReader.OccurredContext)
                : null;
    }

    /// <summary>
    /// Reads a constant the document can state.
    /// </summary>
    /// <param name="expression">The expression the constant was read from.</param>
    /// <param name="semanticModel">The model the expression is read through.</param>
    /// <param name="value">The value of the constant.</param>
    /// <returns>The source, or <see langword="null"/> when the constant has no form in the document.</returns>
    static LiteralSource? ConstantOf(ExpressionSyntax expression, SemanticModel semanticModel, object? value)
    {
        if (EnumConstants.EnumerationOf(expression, semanticModel) is { } enumeration)
        {
            return EnumConstants.TryResolve(enumeration, value, out var member) ? new(member) : null;
        }

        return value is string or bool or int or long or decimal or double ? new(value) : null;
    }
}
