// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Gets the attributes that apply to a member of a type.
/// </summary>
/// <remarks>
/// Commands, events and read models are nearly always positional records, and an attribute written on a positional
/// parameter belongs to the parameter rather than to the property it produces. Reading only the properties would
/// therefore miss every attribute the most common way of writing them declares - which is why this is asked once,
/// here, rather than by each recognizer in turn.
/// </remarks>
public static class MemberAttributes
{
    /// <summary>
    /// Gets the attributes applying to a member.
    /// </summary>
    /// <param name="property">The property to read.</param>
    /// <returns>The attributes, those of the property first.</returns>
    public static IEnumerable<AttributeData> Of(IPropertySymbol property) =>
        ParameterOf(property) is { } parameter
            ? property.GetAttributes().Concat(parameter.GetAttributes())
            : property.GetAttributes();

    /// <summary>
    /// Gets every attribute of a given type applying to a member.
    /// </summary>
    /// <param name="property">The property to read.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name of the attribute.</param>
    /// <returns>The attributes.</returns>
    public static IEnumerable<AttributeData> Of(IPropertySymbol property, string fullMetadataName) =>
        Of(property).Where(_ => _.AttributeClass.Is(fullMetadataName));

    /// <summary>
    /// Determines whether an attribute of a given type applies to a member.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name of the attribute.</param>
    /// <returns>True when the attribute applies.</returns>
    public static bool Has(IPropertySymbol property, string fullMetadataName) =>
        Of(property).Any(_ => _.AttributeClass.Is(fullMetadataName));

    /// <summary>
    /// Gets the positional parameter a property was produced from.
    /// </summary>
    /// <param name="property">The property to read.</param>
    /// <returns>The parameter, or <see langword="null"/> when the property has none.</returns>
    static IParameterSymbol? ParameterOf(IPropertySymbol property) =>
        PrimaryConstructorOf(property.ContainingType)?.Parameters
            .FirstOrDefault(_ => string.Equals(_.Name, property.Name, StringComparison.Ordinal));

    /// <summary>
    /// Gets the constructor a type declares in its own header rather than as a member.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <returns>The constructor, or <see langword="null"/> when the type declares none.</returns>
    static IMethodSymbol? PrimaryConstructorOf(INamedTypeSymbol? type) =>
        type?.InstanceConstructors.FirstOrDefault(_ =>
            _.Parameters.Length > 0 &&
            _.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is TypeDeclarationSyntax));
}
