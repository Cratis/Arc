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
    /// The largest whole number a document's number holds exactly, since numbers are written as doubles.
    /// </summary>
    const long ExactDoubleLimit = 9_007_199_254_740_992;

    /// <summary>
    /// The magnitude below which a decimal is converted to a double and back without overflow.
    /// </summary>
    const decimal ExactDecimalLimit = 1_000_000_000_000_000m;

    /// <summary>
    /// Reads every mapping of a construction.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="semanticModel">The model the construction is read through.</param>
    /// <param name="target">The type being constructed.</param>
    /// <param name="trigger">The parameter carrying the triggering event.</param>
    /// <param name="context">The parameter carrying the event context, if the handler takes it.</param>
    /// <returns>The mappings in the order the type declares its properties, or <see langword="null"/> when any value is code.</returns>
    /// <remarks>
    /// Every property the type carries has to be given a value, and given it directly: a positional record property
    /// through its constructor argument, or an automatic property without an initializer through the object
    /// initializer. A computed property, one with an initializer or an accessor body, or a type deriving from another
    /// carries a value the construction does not state, so the construction stays code.
    /// </remarks>
    public static IReadOnlyList<PropertyMappingModel>? Read(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel,
        INamedTypeSymbol target,
        IParameterSymbol trigger,
        IParameterSymbol? context)
    {
        if (!IsPlain(target) || semanticModel.GetSymbolInfo(creation).Symbol is not IMethodSymbol constructor)
        {
            return null;
        }

        var properties = target.DeclaredProperties().ToList();
        var mapped = new Dictionary<string, PropertyMappingModel>(StringComparer.Ordinal);
        var arguments = creation.ArgumentList?.Arguments ?? default;

        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            var name = argument.NameColon?.Name.Identifier.ValueText ??
                (index < constructor.Parameters.Length ? constructor.Parameters[index].Name : null);
            var property = properties.Find(_ => string.Equals(_.Name, name, StringComparison.Ordinal));
            if (property is null || !IsPositional(property) || !Add(mapped, property, argument.Expression, semanticModel, trigger, context))
            {
                return null;
            }
        }

        foreach (var initialized in creation.Initializer?.Expressions ?? [])
        {
            if (initialized is not AssignmentExpressionSyntax { Left: IdentifierNameSyntax } assignment ||
                semanticModel.GetSymbolInfo(assignment.Left).Symbol is not IPropertySymbol assigned ||
                properties.Find(_ => string.Equals(_.Name, assigned.Name, StringComparison.Ordinal)) is not { } property ||
                !IsAutomatic(property) ||
                !Add(mapped, property, assignment.Right, semanticModel, trigger, context))
            {
                return null;
            }
        }

        return properties.TrueForAll(_ => mapped.ContainsKey(_.Name)) ? [.. properties.Select(_ => mapped[_.Name])] : null;
    }

    /// <summary>
    /// Determines whether constructing a type does nothing but put the values it is given into its properties.
    /// </summary>
    /// <param name="type">The type being constructed.</param>
    /// <returns>True when it derives from nothing but <see langword="object"/> and declares no field of its own.</returns>
    /// <remarks>
    /// A base type receives its values through an initializer that may change them, and a field may be initialized by
    /// code that runs on construction, so either keeps the construction code.
    /// </remarks>
    static bool IsPlain(INamedTypeSymbol type) =>
        type.TypeKind == TypeKind.Class &&
        type.BaseType?.SpecialType == SpecialType.System_Object &&
        !type.GetMembers().OfType<IFieldSymbol>().Any(_ => !_.IsStatic && !_.IsImplicitlyDeclared);

    /// <summary>
    /// Determines whether a property is the one the compiler synthesizes for a positional record parameter.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>True when its value is exactly the argument given for the parameter.</returns>
    /// <remarks>
    /// A positional parameter can also be declared again as a property of its own, initialized from the parameter by
    /// code - <c>public string Name { get; init; } = Name.Trim();</c> - in which case the value it carries is not the
    /// argument. Only the synthesized property is a pass-through.
    /// </remarks>
    static bool IsPositional(IPropertySymbol property) =>
        property.DeclaringSyntaxReferences.Length > 0 &&
        property.DeclaringSyntaxReferences.All(_ => _.GetSyntax() is ParameterSyntax);

    /// <summary>
    /// Determines whether a property is an automatic property that can be set and has no initializer.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>True when assigning it stores exactly the value assigned.</returns>
    static bool IsAutomatic(IPropertySymbol property) =>
        property.SetMethod is not null &&
        property.DeclaringSyntaxReferences.Length > 0 &&
        property.DeclaringSyntaxReferences.All(_ => _.GetSyntax() is PropertyDeclarationSyntax
        {
            Initializer: null,
            ExpressionBody: null,
            AccessorList: { } accessors
        } && accessors.Accessors.All(accessor => accessor is { Body: null, ExpressionBody: null }));

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

        return value switch
        {
            string or bool or int => new(value),
            long number when number is >= -ExactDoubleLimit and <= ExactDoubleLimit => new(value),
            double number when double.IsFinite(number) => new(value),
            decimal number when number is > -ExactDecimalLimit and < ExactDecimalLimit && (decimal)(double)number == number => new(value),
            _ => null
        };
    }
}
