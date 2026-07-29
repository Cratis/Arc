// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Strips everything a value is wrapped in that says how many there are or whether it may be absent.
/// </summary>
/// <remarks>
/// The value a type carries and the wrappers around it answer different questions, and every reader that asks either
/// one has to strip the same wrappers to get the same answer. A collection of an optional concept says one thing about
/// the value and three things about how many there are and whether it may be absent - so a reader marking that value
/// as personal data and a reader naming it have to arrive at the same type, or the mark lands on a name nothing is
/// declared with.
/// </remarks>
public static class UnderlyingTypes
{
    /// <summary>
    /// Strips a type down to the value it carries.
    /// </summary>
    /// <param name="type">The type to strip.</param>
    /// <param name="optional">Set when a wrapper said the value may be absent.</param>
    /// <param name="collection">Set when the value is a collection of what is left.</param>
    /// <returns>The type of the value itself.</returns>
    public static ITypeSymbol Of(ITypeSymbol type, ref bool optional, ref bool collection)
    {
        var current = Unwrap(type, ref optional);
        var element = CollectionElements.ElementOf(current);
        if (element is null)
        {
            return current;
        }

        collection = true;

        return Unwrap(element, ref optional);
    }

    /// <summary>
    /// Strips a type down to the value it carries, when nothing is asked about the wrappers.
    /// </summary>
    /// <param name="type">The type to strip.</param>
    /// <returns>The type of the value itself.</returns>
    public static ITypeSymbol Of(ITypeSymbol type)
    {
        var optional = false;
        var collection = false;

        return Of(type, ref optional, ref collection);
    }

    /// <summary>
    /// Strips the wrappers that only say whether a value may be absent.
    /// </summary>
    /// <param name="type">The type to strip.</param>
    /// <param name="optional">Set when a wrapper said the value may be absent.</param>
    /// <returns>The wrapped type.</returns>
    static ITypeSymbol Unwrap(ITypeSymbol type, ref bool optional)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            optional = true;

            return nullable.TypeArguments[0];
        }

        if (type.NullableAnnotation == NullableAnnotation.Annotated && type.IsReferenceType)
        {
            optional = true;
        }

        return type;
    }
}
