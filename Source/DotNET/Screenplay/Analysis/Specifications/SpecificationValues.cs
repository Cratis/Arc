// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads the values a specification states for one artifact, from the construction stating them.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unreadable is reported to.</param>
/// <param name="identities">The <see cref="GeneratedIdentities"/> telling an identity that was made from a value that was computed.</param>
/// <remarks>
/// A specification is closed - it refers to nothing outside itself - so a constant is the only source of a value
/// there is. That is the same discipline a produces mapping is read with, one source shorter: a produces mapping may
/// also name the input of the command being handled, and a scenario has no input to name.
/// </remarks>
public class SpecificationValues(ScreenplayDiagnostics diagnostics, GeneratedIdentities identities)
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
        SpecificationDraft draft)
    {
        var values = new List<PropertyMappingModel>();
        var constructor = semanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;

        foreach (var (name, expression) in Stated(creation, constructor))
        {
            Add(values, PropertyOf(type, name), expression, semanticModel, type, specification, location, draft);
        }

        return values;
    }

    /// <summary>
    /// Gets every value a construction states, from its arguments and from its initializer.
    /// </summary>
    /// <param name="creation">The construction to read.</param>
    /// <param name="constructor">The constructor being called.</param>
    /// <returns>The values, each with the name it fills in, in the order the source declares them.</returns>
    static IEnumerable<(string Name, ExpressionSyntax Expression)> Stated(
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

        foreach (var assignment in creation.Initializer?.Expressions.OfType<AssignmentExpressionSyntax>() ?? [])
        {
            if (assignment.Left is IdentifierNameSyntax identifier)
            {
                yield return (identifier.Identifier.ValueText, assignment.Right);
            }
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
    /// <returns>The property name.</returns>
    static string PropertyOf(ITypeSymbol type, string name) =>
        type.DeclaredProperties().FirstOrDefault(_ => string.Equals(_.Name, name, StringComparison.OrdinalIgnoreCase))?.Name ?? name;

    /// <summary>
    /// Adds a value, reporting one that is code rather than stating it as something it is not.
    /// </summary>
    /// <param name="values">The values collected so far.</param>
    /// <param name="property">The property being filled in.</param>
    /// <param name="expression">The expression filling it in.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="type">The type being constructed.</param>
    /// <param name="specification">The name of the specification.</param>
    /// <param name="location">Where the specification lives.</param>
    /// <param name="draft">The scenario collecting exact value evidence.</param>
    /// <remarks>
    /// An identity made on the spot is left out without a word, because there is no value for the document to have
    /// missed - see <see cref="GeneratedIdentities"/>. Every other value that cannot be read is one the source states
    /// and the document does not, which is the difference worth reading.
    /// </remarks>
    void Add(
        List<PropertyMappingModel> values,
        string property,
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol type,
        string specification,
        string location,
        SpecificationDraft draft)
    {
        if (_sources.Read(expression, semanticModel, type, location) is LiteralSource literal)
        {
            var value = new PropertyMappingModel(property, literal);
            values.Add(value);
            draft.AddValue(value, expression.GetLocation());
            return;
        }

        if (identities.Yields(expression, semanticModel))
        {
            return;
        }

        diagnostics.Information(
            ScreenplayDiagnosticCodes.UnreadableSpecificationValue,
            $"The value '{specification}' states for '{type.Name}.{property}' is code rather than a constant, so the scenario states everything but that value",
            location);
    }
}
