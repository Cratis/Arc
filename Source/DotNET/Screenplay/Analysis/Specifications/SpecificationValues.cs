// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads the values a specification states for one artifact, from the construction stating them.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unreadable is reported to.</param>
/// <param name="identities">The <see cref="GeneratedIdentities"/> telling an identity that was made from a value that was computed.</param>
/// <param name="models">The models of every analyzed project, or null for a single-compilation reading.</param>
/// <remarks>
/// A specification is closed - it refers to nothing outside itself - so a constant is the only source of a value
/// there is. That is the same discipline a produces mapping is read with, one source shorter: a produces mapping may
/// also name the input of the command being handled, and a scenario has no input to name.
/// </remarks>
public class SpecificationValues(ScreenplayDiagnostics diagnostics, GeneratedIdentities identities, SemanticModels? models = null)
{
    readonly MappingSourceReader _sources = new(diagnostics);

    /// <summary>
    /// Reads the values one construction states.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the construction lives in.</param>
    /// <param name="type">The type being constructed.</param>
    /// <param name="specification">The name of the specification, for use in diagnostics.</param>
    /// <param name="location">Where the specification lives, for use in diagnostics.</param>
    /// <param name="draft">The scenario collecting exact value evidence.</param>
    /// <returns>The values, in the order the source declares them.</returns>
    public IEnumerable<PropertyMappingModel> Read(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel,
        ITypeSymbol type,
        string specification,
        string location,
        SpecificationDraft draft) =>
        Read(creation, new SpecificationValueContext(semanticModel, type, type, specification, location, draft, QueryValues: false));

    /// <summary>
    /// Reads the values one exact Stage-generated query result construction states.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the construction lives in.</param>
    /// <param name="type">The type being constructed in the specification compilation.</param>
    /// <param name="sourceType">The corresponding exact type declared by the application.</param>
    /// <param name="specification">The name of the specification, for use in diagnostics.</param>
    /// <param name="location">Where the specification lives, for use in diagnostics.</param>
    /// <param name="draft">The scenario collecting exact value evidence.</param>
    /// <returns>The values, in the order the source declares them.</returns>
    /// <remarks>
    /// Query specifications additionally admit the direct concept and framework parse syntax Stage emits. Existing
    /// command, event, and read-model readers continue through the legacy path and retain their original source
    /// contract.
    /// </remarks>
    public IEnumerable<PropertyMappingModel> ReadQuery(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel,
        ITypeSymbol type,
        ITypeSymbol sourceType,
        string specification,
        string location,
        SpecificationDraft draft) =>
        Read(creation, new SpecificationValueContext(semanticModel, type, sourceType, specification, location, draft, QueryValues: true));

    /// <summary>
    /// Gets every value a construction states, from its arguments and from its initializer.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="constructor">The constructor being called.</param>
    /// <returns>The values, each with the name it fills in, in the order the source declares them.</returns>
    internal static IEnumerable<(string Name, ExpressionSyntax Expression)> Stated(
        BaseObjectCreationExpressionSyntax creation,
        IMethodSymbol? constructor)
    {
        return StatedArguments(creation, constructor).Concat(StatedInitializers(creation));
    }

    /// <summary>
    /// Gets the values a construction states through its arguments.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="constructor">The constructor being called.</param>
    /// <returns>The values, each with the name it fills in.</returns>
    static IEnumerable<(string Name, ExpressionSyntax Expression)> StatedArguments(
        BaseObjectCreationExpressionSyntax creation,
        IMethodSymbol? constructor)
    {
        var arguments = creation.ArgumentList?.Arguments ?? [];
        for (var index = 0; index < arguments.Count; index++)
        {
            if (NameOf(arguments[index], constructor, index) is { } name)
            {
                yield return (name, arguments[index].Expression);
            }
        }
    }

    /// <summary>
    /// Gets the values a construction states through its initializer.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <returns>The values, each with the name it fills in.</returns>
    static IEnumerable<(string Name, ExpressionSyntax Expression)> StatedInitializers(BaseObjectCreationExpressionSyntax creation)
    {
        var assignments = creation.Initializer?.Expressions.OfType<AssignmentExpressionSyntax>() ?? [];
        foreach (var assignment in assignments.Where(assignment => assignment.Left is IdentifierNameSyntax))
        {
            yield return (((IdentifierNameSyntax)assignment.Left).Identifier.ValueText, assignment.Right);
        }
    }

    /// <summary>
    /// Gets the name of the property an argument fills in.
    /// </summary>
    /// <param name="argument">The argument to name.</param>
    /// <param name="constructor">The constructor being called.</param>
    /// <param name="index">The position of the argument.</param>
    /// <returns>The parameter name, or <see langword="null"/> when it cannot be resolved.</returns>
    static string? NameOf(ArgumentSyntax argument, IMethodSymbol? constructor, int index)
    {
        if (argument.NameColon is { } named)
        {
            return named.Name.Identifier.ValueText;
        }

        return constructor is not null && index < constructor.Parameters.Length ? constructor.Parameters[index].Name : null;
    }

    /// <summary>
    /// Resolves the declared casing of a property from the name an argument used.
    /// </summary>
    /// <param name="type">The type being constructed.</param>
    /// <param name="name">The name to resolve.</param>
    /// <param name="exact">Whether only exact Stage-authored casing is admitted.</param>
    /// <returns>The property name.</returns>
    static string PropertyOf(ITypeSymbol type, string name, bool exact) =>
        type.DeclaredProperties().FirstOrDefault(_ => string.Equals(
            _.Name,
            name,
            exact ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))?.Name ?? name;

    static void Complete(ITypeSymbol type, List<PropertyMappingModel> values, SpecificationDraft draft)
    {
        foreach (var property in type.DeclaredProperties().Where(property => values.TrueForAll(value => value.Property != property.Name)))
        {
            draft.CannotRead($"it cannot prove a stateable value for '{type.Name}.{property.Name}', and the Screenplay binder requires every fixture property");
        }
    }

    static bool CanStateNull(ITypeSymbol type, IPropertySymbol property) =>
        !CommandReader.IsCommand(type) && !EventReader.IsEvent(type) &&
        (property.Type.NullableAnnotation == NullableAnnotation.Annotated ||
         property.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T });

    static IPropertySymbol? DeclaredProperty(ITypeSymbol type, string property) =>
        type.DeclaredProperties().SingleOrDefault(_ => string.Equals(_.Name, property, StringComparison.Ordinal));

    static string NullNotAccepted(ITypeSymbol type, string property) =>
        $"it states '{type.Name}.{property}' as null, which the Screenplay binder does not accept in this fixture";

    static string? ConstantRefusal(ExpressionSyntax expression, string property, IPropertySymbol? localProperty, SpecificationValueContext context)
    {
        if (localProperty is null ||
            context.SemanticModel.GetConstantValue(MappingSourceReader.Unwrap(expression)) is not { HasValue: true } constant)
        {
            return null;
        }

        if (!StatableValues.TryState(localProperty.Type, constant.Value, out _))
        {
            return $"it states {StatableValues.WhyNot(context.Type, localProperty, constant.Value)}";
        }

        return constant.Value is null && !CanStateNull(context.Type, localProperty) ? NullNotAccepted(context.Type, property) : null;
    }

    static void AddMapping(List<PropertyMappingModel> values, PropertyMappingModel mapping, ExpressionSyntax expression, SpecificationDraft draft)
    {
        values.Add(mapping);
        draft.AddValue(mapping, expression.GetLocation());
    }

    static bool RefusesNull(LiteralSource? literal, string property, SpecificationValueContext context)
    {
        if (literal is not { Value: null } || context.QueryValues)
        {
            return false;
        }

        var localProperty = DeclaredProperty(context.Type, property);
        if (localProperty is null || CanStateNull(context.Type, localProperty))
        {
            return false;
        }

        context.Draft.CannotRead(NullNotAccepted(context.Type, property));
        return true;
    }

    List<PropertyMappingModel> Read(BaseObjectCreationExpressionSyntax creation, SpecificationValueContext context)
    {
        var values = new List<PropertyMappingModel>();
        var constructor = context.SemanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;

        foreach (var (name, expression) in Stated(creation, constructor))
        {
            Add(values, PropertyOf(context.Type, name, context.QueryValues), expression, context);
        }

        if (!context.QueryValues)
        {
            Complete(context.Type, values, context.Draft);
        }

        return values;
    }

    bool PreservesConversion(ExpressionSyntax expression, string property, SpecificationValueContext context)
    {
        if (ValuePreservingConversions.Preserves(expression, context.SemanticModel, models, trustFrameworkMetadata: true))
        {
            return true;
        }

        context.Draft.CannotRead($"it cannot prove that a user-defined conversion for '{context.Type.Name}.{property}' forwards its backing value unchanged");
        return false;
    }

    /// <summary>
    /// Adds a value a case table binds, when it binds one.
    /// </summary>
    /// <param name="values">The values collected so far.</param>
    /// <param name="property">The property being filled in.</param>
    /// <param name="expression">The expression filling it in.</param>
    /// <param name="context">What the value is read against.</param>
    /// <returns>True when the case table decided the value, whether or not it could be added.</returns>
    bool TryAddCaseValue(List<PropertyMappingModel> values, string property, ExpressionSyntax expression, SpecificationValueContext context)
    {
        var localProperty = DeclaredProperty(context.Type, property);
        var draft = context.Draft;
        if (localProperty is null || draft.CaseValues?.TryRead(
                expression, context.SemanticModel, localProperty.Type, CanStateNull(context.Type, localProperty), out var bound) != true)
        {
            return false;
        }

        if (bound is null)
        {
            draft.CannotRead(draft.CaseValues.Failure!);
        }
        else if (PreservesConversion(expression, property, context))
        {
            AddMapping(values, new PropertyMappingModel(property, bound), expression, draft);
        }

        return true;
    }

    /// <summary>
    /// Reads the literal an expression states, when it states one.
    /// </summary>
    /// <param name="expression">The expression filling the property in.</param>
    /// <param name="property">The property being filled in.</param>
    /// <param name="context">What the value is read against.</param>
    /// <returns>The literal, if any, and whether the scenario was refused because of the value.</returns>
    (LiteralSource? Literal, bool Refused) ReadLiteral(ExpressionSyntax expression, string property, SpecificationValueContext context)
    {
        var localProperty = DeclaredProperty(context.Type, property);
        var sourceProperty = DeclaredProperty(context.SourceType, property);
        if (context.QueryValues)
        {
            var queryLiteral = localProperty is not null && sourceProperty is not null
                ? _sources.ReadQueryLiteral(expression, context.SemanticModel, localProperty.Type, sourceProperty.Type)
                : null;
            return (queryLiteral, false);
        }

        if (ConstantRefusal(expression, property, localProperty, context) is { } refusal)
        {
            context.Draft.CannotRead(refusal);
            return (null, true);
        }

        return ReadSourceLiteral(expression, property, localProperty, context);
    }

    (LiteralSource? Literal, bool Refused) ReadSourceLiteral(ExpressionSyntax expression, string property, IPropertySymbol? localProperty, SpecificationValueContext context)
    {
        string? reason = null;
        var literal = localProperty is not null
            ? SpecificationCollections.Read(expression, context.SemanticModel, localProperty.Type, _sources, out reason, models)
            : null;
        if (reason is not null)
        {
            context.Draft.CannotRead($"it states '{context.Type.Name}.{property}' with {reason}");
            return (null, true);
        }

        literal ??= _sources.Read(expression, context.SemanticModel, context.Type, context.Location) as LiteralSource;
        if (literal is null && localProperty is not null)
        {
            literal = ReadRenderedCommandLiteral(expression, localProperty, context);
        }

        return (literal, false);
    }

    LiteralSource? ReadRenderedCommandLiteral(ExpressionSyntax expression, IPropertySymbol localProperty, SpecificationValueContext context) =>
        context.Draft.RenderedCommand?.IsCommand(context.Type) == true
            ? context.Draft.RenderedCommand.ReadLiteral(expression, localProperty.Type, _sources, context.SemanticModel)
            : null;

    /// <summary>
    /// Adds a value, reporting one that is code rather than stating it as something it is not.
    /// </summary>
    /// <param name="values">The values collected so far.</param>
    /// <param name="property">The property being filled in.</param>
    /// <param name="expression">The expression filling it in.</param>
    /// <param name="context">What the value is read against.</param>
    /// <remarks>
    /// An identity made on the spot is left out without a word, because there is no value for the document to have
    /// missed - see <see cref="GeneratedIdentities"/>. A value that is code is one the source states and the document
    /// does not, which is the difference worth reading. A constant the document cannot hold at all - a number no
    /// member of an enumeration is declared with (including <see langword="default"/> for one with no zero member and flags
    /// combined into a value no member is declared with), or <see langword="null"/> for a required property - takes the
    /// whole scenario with it, because what it issues or starts from would no longer be what was written.
    /// </remarks>
    void Add(List<PropertyMappingModel> values, string property, ExpressionSyntax expression, SpecificationValueContext context)
    {
        if (TryAddCaseValue(values, property, expression, context) || !PreservesConversion(expression, property, context))
        {
            return;
        }

        var (literal, refused) = ReadLiteral(expression, property, context);
        if (refused || RefusesNull(literal, property, context))
        {
            return;
        }

        if (literal is not null)
        {
            AddMapping(values, new PropertyMappingModel(property, literal), expression, context.Draft);
            return;
        }

        if (!identities.Yields(expression, context.SemanticModel))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnreadableSpecificationValue,
                $"The value '{context.Specification}' states for '{context.Type.Name}.{property}' is code rather than a constant, so the scenario states everything but that value",
                context.Location);
        }
    }
}
