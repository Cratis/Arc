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
    /// <summary>
    /// Reads the mappings of a single event construction.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the construction lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read, if it is not the handler's own.</param>
    /// <returns>The mappings, in the order the source declares them.</returns>
    public IEnumerable<PropertyMappingModel> Read(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        ITypeSymbol eventType,
        string location,
        ParameterBindings? bindings = null)
    {
        var mappings = new List<PropertyMappingModel>();
        var constructor = semanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;

        ReadArguments(creation, constructor, semanticModel, owner, eventType, location, mappings, bindings);
        ReadInitializer(creation, semanticModel, owner, eventType, location, mappings, bindings);

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

    /// <summary>
    /// Reads the mappings the constructor arguments declare.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="constructor">The constructor being called.</param>
    /// <param name="semanticModel">The semantic model of the tree the construction lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="mappings">The mappings collected so far.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read.</param>
    void ReadArguments(
        BaseObjectCreationExpressionSyntax creation,
        IMethodSymbol? constructor,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        ITypeSymbol eventType,
        string location,
        List<PropertyMappingModel> mappings,
        ParameterBindings? bindings)
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
                Report(eventType, $"argument {index + 1}", location);
                continue;
            }

            Add(mappings, PropertyOf(eventType, name), argument.Expression, semanticModel, owner, eventType, location, bindings);
        }
    }

    /// <summary>
    /// Reads the mappings an object initializer declares.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the construction lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="mappings">The mappings collected so far.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read.</param>
    void ReadInitializer(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        ITypeSymbol eventType,
        string location,
        List<PropertyMappingModel> mappings,
        ParameterBindings? bindings)
    {
        foreach (var assignment in creation.Initializer?.Expressions.OfType<AssignmentExpressionSyntax>() ?? [])
        {
            if (assignment.Left is not IdentifierNameSyntax identifier)
            {
                Report(eventType, assignment.Left.ToString(), location);
                continue;
            }

            Add(mappings, PropertyOf(eventType, identifier.Identifier.ValueText), assignment.Right, semanticModel, owner, eventType, location, bindings);
        }
    }

    /// <summary>
    /// Adds a mapping, reporting an expression that could not be expressed rather than guessing at it.
    /// </summary>
    /// <param name="mappings">The mappings collected so far.</param>
    /// <param name="property">The property being filled in.</param>
    /// <param name="expression">The expression filling it in.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read.</param>
    void Add(
        List<PropertyMappingModel> mappings,
        string property,
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        ITypeSymbol eventType,
        string location,
        ParameterBindings? bindings)
    {
        var source = MappingSourceReader.Read(expression, semanticModel, owner, bindings);
        if (source is null)
        {
            Report(eventType, property, location);
            return;
        }

        mappings.Add(new(property, source));
    }

    /// <summary>
    /// Reports a mapping that could not be expressed.
    /// </summary>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="property">The property that was being filled in.</param>
    /// <param name="location">Where the command lives.</param>
    void Report(ITypeSymbol eventType, string property, string location) =>
        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnmappableCommandProduction,
            $"The value given to '{eventType.Name}.{property}' is code rather than command input or a constant, so the mapping was left out",
            location);
}
