// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Determines whether a type is a collection, and of what.
/// </summary>
/// <remarks>
/// A string is a sequence of characters as far as the type system is concerned, which is exactly the trap this
/// exists to avoid - it is a value, never a collection.
/// </remarks>
public static class CollectionElements
{
    /// <summary>
    /// Gets the element type of a collection.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The element type, or <see langword="null"/> when the type is not a collection.</returns>
    public static ITypeSymbol? ElementOf(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return null;
        }

        if (type is IArrayTypeSymbol array)
        {
            return array.ElementType;
        }

        if (type is not INamedTypeSymbol named)
        {
            return null;
        }

        if (named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
        {
            return named.TypeArguments[0];
        }

        var enumerable = named.AllInterfaces.FirstOrDefault(_ =>
            _.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

        return enumerable?.TypeArguments[0];
    }
}
