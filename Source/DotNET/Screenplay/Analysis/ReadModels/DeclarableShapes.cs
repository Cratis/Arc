// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.ReadModels;

/// <summary>
/// Decides whether every value a read model holds can be typed by a declaration saying what that value is.
/// </summary>
/// <param name="types">The <see cref="TypeRegistry"/> holding the concepts and records declared so far.</param>
/// <param name="readModels">The declaration names read models are declared under.</param>
/// <param name="naming">The <see cref="IScreenplayNaming"/> turning a simple name into the declaration name.</param>
/// <remarks>
/// A read model property is written with a type, and the type has to resolve - to a primitive, to a concept or to a
/// <c>type</c> declaration - or the declaration names something the document never introduces and the executable
/// model has nothing to bind it to. Those are exactly the types the rest of the generator declares: a framework type
/// with a Screenplay primitive, an enumeration or a <c>ConceptAs</c>, and a record carrying at least one value of the
/// same kinds.
/// <para>
/// Resolving is not enough on its own. Concepts and records are declared under their simple name, the first one
/// registered keeping it, so a value whose type shares its declaration name with a different type already declared -
/// or with another type the read model holds, or with a read model - would be written with a name stating someone
/// else's shape. A collection whose elements may be null cannot be stated either: <c>optional</c> on a collection says
/// the collection may be absent, never that an element may be. Every one of these is asked before anything is read,
/// because reading a property registers what it reaches, and a read model the document does not declare must not
/// commit it to anything.
/// </para>
/// </remarks>
public class DeclarableShapes(TypeRegistry types, IReadOnlySet<string> readModels, IScreenplayNaming naming)
{
    /// <summary>
    /// Finds the first value a read model holds that no declaration could faithfully type.
    /// </summary>
    /// <param name="type">The read model type.</param>
    /// <returns>The path of the property and why it cannot be typed, or <see langword="null"/> when every value can.</returns>
    public (string Property, string Reason)? FirstUndeclarable(ITypeSymbol type) =>
        FirstUndeclarable(type, string.Empty, new Dictionary<string, string>(StringComparer.Ordinal));

    /// <summary>
    /// Determines whether a value holds a collection whose elements may be null.
    /// </summary>
    /// <param name="type">The type of the value.</param>
    /// <returns>True when it does.</returns>
    static bool HoldsNullableElements(ITypeSymbol type)
    {
        var current = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0]
            : type.WithNullableAnnotation(NullableAnnotation.None);

        return CollectionElements.ElementOf(current) is { } element &&
            (element is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } ||
            element.NullableAnnotation == NullableAnnotation.Annotated);
    }

    /// <summary>
    /// Determines whether a type is written as a Screenplay primitive.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>True when it is.</returns>
    static bool IsPrimitive(ITypeSymbol type) =>
        type is INamedTypeSymbol named && ScreenplayPrimitiveTypes.TryResolve(named.FullMetadataName(), out _);

    /// <summary>
    /// Determines whether a type is declared as a concept.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>True when it is.</returns>
    static bool IsConcept(ITypeSymbol type) =>
        type.TypeKind == TypeKind.Enum || type.FindBase(WellKnownTypeNames.ConceptAs) is not null;

    /// <summary>
    /// Finds the first value of a type no declaration could faithfully type, walking every record it holds.
    /// </summary>
    /// <param name="type">The type to walk.</param>
    /// <param name="path">The path of the property holding the type, empty for the read model itself.</param>
    /// <param name="reached">The full name of every concept and record reached so far, by declaration name.</param>
    /// <returns>The path of the property and why it cannot be typed, or <see langword="null"/> when every value can.</returns>
    (string Property, string Reason)? FirstUndeclarable(ITypeSymbol type, string path, Dictionary<string, string> reached)
    {
        foreach (var property in type.DeclaredProperties())
        {
            var at = $"{path}{property.Name}";
            if (HoldsNullableElements(property.Type))
            {
                return (at, "as a collection whose elements may be null, which a declaration cannot say - 'optional' on a collection says only that the collection may be absent");
            }

            var carried = UnderlyingTypes.Of(property.Type);
            if (IsPrimitive(carried))
            {
                continue;
            }

            var isConcept = IsConcept(carried);
            if (!isConcept && !CarriedTypes.IsRecord(carried))
            {
                return (at, $"as '{carried.ToDisplayString()}', which the document has no type for");
            }

            var full = carried.ToDisplayString();
            var name = naming.ToDeclarationName(carried.Name);
            if (reached.TryGetValue(name, out var other))
            {
                if (string.Equals(other, full, StringComparison.Ordinal))
                {
                    continue;
                }

                return (at, $"as '{full}', and it also holds '{other}', which is declared under the same name '{name}'");
            }

            reached[name] = full;

            if (readModels.Contains(name))
            {
                return (at, $"as '{full}', whose name '{name}' is the name of a read model");
            }

            if (!types.WouldResolveTo(carried) ||
                types.Names.Any(_ => !string.Equals(_, carried.Name, StringComparison.Ordinal) && naming.ToDeclarationName(_) == name))
            {
                return (at, $"as '{full}', and the document already declares something else as '{name}'");
            }

            if (isConcept)
            {
                continue;
            }

            if (!carried.DeclaredProperties().Any())
            {
                return (at, $"as '{full}', which the document has no type for");
            }

            if (FirstUndeclarable(carried, $"{at}.", reached) is { } undeclarable)
            {
                return undeclarable;
            }
        }

        return null;
    }
}
