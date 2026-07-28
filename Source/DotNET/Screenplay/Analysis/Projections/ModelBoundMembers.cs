// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Gets the attributes declared on the members of a model-bound read model.
/// </summary>
/// <remarks>
/// A read model is nearly always a positional record, and an attribute written on a positional parameter belongs to
/// the parameter rather than to the property it produces. Reading only the properties would therefore miss every
/// mapping the most common way of writing a read model declares.
/// </remarks>
public static class ModelBoundMembers
{
    /// <summary>
    /// Gets the attributes applying to a member of a read model.
    /// </summary>
    /// <param name="property">The property to read.</param>
    /// <returns>The attributes, those of the property first.</returns>
    public static IEnumerable<AttributeData> AttributesOf(IPropertySymbol property) =>
        ParameterOf(property) is { } parameter
            ? property.GetAttributes().Concat(parameter.GetAttributes())
            : property.GetAttributes();

    /// <summary>
    /// Determines whether an attribute of a given type applies to a member of a read model.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name of the attribute.</param>
    /// <returns>True when the attribute applies.</returns>
    public static bool HasAttribute(IPropertySymbol property, string fullMetadataName) =>
        AttributesOf(property).Any(_ => _.AttributeClass.Is(fullMetadataName));

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
