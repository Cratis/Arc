// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Types;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.ReadModels;

/// <summary>
/// Decides whether every value a read model holds can be typed by something the document declares.
/// </summary>
/// <remarks>
/// A read model property is written with a type, and the type has to resolve - to a primitive, to a concept or to a
/// <c>type</c> declaration - or the declaration names something the document never introduces and the executable
/// model has nothing to bind it to. Those are exactly the types the rest of the generator declares: a framework type
/// with a Screenplay primitive, an enumeration or a <c>ConceptAs</c>, and a record carrying at least one value of the
/// same kinds. Asking before anything is read is what keeps a read model the document cannot declare from committing
/// it to the concepts and shapes it reaches.
/// </remarks>
public static class DeclarableShapes
{
    /// <summary>
    /// Finds the first property of a read model the document cannot type.
    /// </summary>
    /// <param name="type">The read model type.</param>
    /// <returns>The property and its type, or <see langword="null"/> when every property can be typed.</returns>
    public static (string Property, string Type)? FirstUndeclarable(ITypeSymbol type) =>
        FirstUndeclarable(type, new HashSet<string>(StringComparer.Ordinal) { type.ToDisplayString() });

    static (string Property, string Type)? FirstUndeclarable(ITypeSymbol type, HashSet<string> walked)
    {
        foreach (var property in type.DeclaredProperties())
        {
            var carried = UnderlyingTypes.Of(property.Type);
            if (!IsDeclarable(carried, walked))
            {
                return (property.Name, carried.ToDisplayString());
            }
        }

        return null;
    }

    static bool IsDeclarable(ITypeSymbol type, HashSet<string> walked)
    {
        if ((type is INamedTypeSymbol named && ScreenplayPrimitiveTypes.TryResolve(named.FullMetadataName(), out _)) ||
            type.TypeKind == TypeKind.Enum ||
            type.FindBase(WellKnownTypeNames.ConceptAs) is not null)
        {
            return true;
        }

        if (!CarriedTypes.IsRecord(type))
        {
            return false;
        }

        // A record reached again around a loop is already being answered for further up.
        return !walked.Add(type.ToDisplayString()) ||
            (type.DeclaredProperties().Any() && FirstUndeclarable(type, walked) is null);
    }
}
