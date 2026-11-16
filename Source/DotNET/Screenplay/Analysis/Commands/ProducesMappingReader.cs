// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads the mappings from a command's input onto the properties of an event it constructs.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// This is what reading source buys over reading metadata - the arguments handed to an event's constructor say
/// exactly where each property's value comes from, rather than being guessed at by matching names.
/// </remarks>
public class ProducesMappingReader(ScreenplayDiagnostics diagnostics)
{
    readonly MappingSourceReader _sources = new(diagnostics);
    readonly List<string> _codeMappedProperties = [];
    AuthoringSources? _authoring;

    /// <summary>Gets the properties whose mappings stayed in code during the last read.</summary>
    internal IReadOnlyList<string> CodeMappedProperties => _codeMappedProperties;

    /// <summary>Gets or sets the controller's already-classified request input, for this isolated reader.</summary>
    internal IParameterSymbol? ControllerInput
    {
        get => _sources.ControllerInput;
        set => _sources.ControllerInput = value;
    }

    /// <summary>
    /// Reads the mappings of a single event construction.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the construction lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read, if it is not the handler's own.</param>
    /// <param name="authoring">Proven generated values and read dependencies.</param>
    /// <returns>The mappings, in the order the source declares them.</returns>
    public IEnumerable<PropertyMappingModel> Read(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        ITypeSymbol eventType,
        string location,
        ParameterBindings? bindings = null,
        AuthoringSources? authoring = null)
    {
        _authoring = authoring;
        _codeMappedProperties.Clear();
        var mappings = new List<PropertyMappingModel>();
        var constructor = semanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;

        var context = new MappingContext(semanticModel, owner, eventType, location, bindings);
        ReadArguments(creation, constructor, context, mappings);
        ReadInitializer(creation, context, mappings);

        return mappings;
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
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="name">The name to resolve.</param>
    /// <returns>The property name.</returns>
    static string PropertyOf(ITypeSymbol eventType, string name) =>
        eventType.DeclaredProperties().FirstOrDefault(_ => string.Equals(_.Name, name, StringComparison.OrdinalIgnoreCase))?.Name ?? name;

    static bool IsPropertyValue(ExpressionSyntax expression, string property, string path, MappingContext context) =>
        !path.Contains('.', StringComparison.Ordinal) && SymbolEqualityComparer.Default.Equals(
            context.SemanticModel.GetTypeInfo(MappingSourceReader.Unwrap(expression)).Type,
            context.EventType.DeclaredProperties().FirstOrDefault(target => target.Name == property)?.Type);

    static bool IsReadDependency(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var current = MappingSourceReader.Unwrap(expression);
        while (current is MemberAccessExpressionSyntax member)
        {
            current = MappingSourceReader.Unwrap(member.Expression);
        }

        return semanticModel.GetSymbolInfo(current).Symbol is IParameterSymbol parameter && !parameter.HasAttribute(WellKnownTypeNames.GeneratedValueAttribute);
    }

    /// <summary>
    /// Reads the mappings the constructor arguments declare.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="constructor">The constructor being called.</param>
    /// <param name="context">The source and target of the mappings being read.</param>
    /// <param name="mappings">The mappings collected so far.</param>
    void ReadArguments(
        BaseObjectCreationExpressionSyntax creation,
        IMethodSymbol? constructor,
        MappingContext context,
        List<PropertyMappingModel> mappings)
    {
        var arguments = creation.ArgumentList?.Arguments;
        if (arguments is null)
        {
            return;
        }

        for (var index = 0; index < arguments.Value.Count; index++)
        {
            var argument = arguments.Value[index];
            var name = NameOf(argument, constructor, index);
            if (name is null)
            {
                Report(context.EventType, $"argument {index + 1}", context.Location);
                continue;
            }

            Add(mappings, PropertyOf(context.EventType, name), argument.Expression, context);
        }
    }

    /// <summary>
    /// Reads the mappings an object initializer declares.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="context">The source and target of the mappings being read.</param>
    /// <param name="mappings">The mappings collected so far.</param>
    void ReadInitializer(
        BaseObjectCreationExpressionSyntax creation,
        MappingContext context,
        List<PropertyMappingModel> mappings)
    {
        foreach (var assignment in creation.Initializer?.Expressions.OfType<AssignmentExpressionSyntax>() ?? [])
        {
            if (assignment.Left is not IdentifierNameSyntax identifier)
            {
                Report(context.EventType, assignment.Left.ToString(), context.Location);
                continue;
            }

            Add(mappings, PropertyOf(context.EventType, identifier.Identifier.ValueText), assignment.Right, context);
        }
    }

    /// <summary>
    /// Adds a mapping, reporting an expression that could not be expressed rather than guessing at it.
    /// </summary>
    /// <param name="mappings">The mappings collected so far.</param>
    /// <param name="property">The property being filled in.</param>
    /// <param name="expression">The expression filling it in.</param>
    /// <param name="context">The source and target of the mappings being read.</param>
    void Add(
        List<PropertyMappingModel> mappings,
        string property,
        ExpressionSyntax expression,
        MappingContext context)
    {
        if (_authoring?.UnavailableIn(expression, context.SemanticModel, context.Bindings) is { } unavailable)
        {
            mappings.Add(new(property, new UnavailableValueSource(unavailable)));
            return;
        }

        var source = _sources.Read(expression, context.SemanticModel, context.Owner, context.Location, context.Bindings) ??
            ReadAuthoringSource(expression, property, context);
        if (source is null)
        {
            Report(context.EventType, property, context.Location);
            return;
        }

        mappings.Add(new(property, source));
    }

    PropertyPathSource? ReadAuthoringSource(ExpressionSyntax expression, string property, MappingContext context)
    {
        if (_authoring?.ReadPath(expression, context.SemanticModel) is not { } path)
        {
            return null;
        }

        return IsPropertyValue(expression, property, path, context) || IsReadDependency(expression, context.SemanticModel)
            ? new PropertyPathSource(path)
            : null;
    }

    /// <summary>
    /// Reports a mapping that could not be expressed.
    /// </summary>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="property">The property that was being filled in.</param>
    /// <param name="location">Where the command lives.</param>
    void Report(ITypeSymbol eventType, string property, string location)
    {
        _codeMappedProperties.Add(property);
        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnmappableCommandProduction,
            $"The value given to '{eventType.Name}.{property}' is code rather than command input or a constant, so the mapping was left out",
            location);
    }

    sealed record MappingContext(SemanticModel SemanticModel, ITypeSymbol Owner, ITypeSymbol EventType, string Location, ParameterBindings? Bindings);
}
