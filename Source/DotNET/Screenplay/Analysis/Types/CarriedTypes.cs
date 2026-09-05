// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Types;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Finds every type a record carries, however far down it is carried.
/// </summary>
/// <remarks>
/// A concept is declared once at the top of a document and referred to by name, and a concept nothing refers to has no
/// reason to be declared - which is why only the types artifacts really name are collected. Reaching only the outermost
/// position took that too far: a name carried inside a line of an approved timesheet is referred to by the application
/// just as much as one written straight onto an event, and a document leaving it out understates what the application
/// holds. Where that value is marked as personal data, the document then understates something the reader is answerable
/// for, which is the opposite of what declaring concepts is for.
/// </remarks>
public static class CarriedTypes
{
    /// <summary>
    /// Determines whether a type is a record carrying values rather than being one.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a record whose members are worth walking.</returns>
    /// <remarks>
    /// A record is what a value carrying several values is written as, and stopping at that is what keeps the walk
    /// finite and about the application. Following every class a property mentions would descend into the framework
    /// types the application merely touches, and a constructed generic is a type the document cannot name at all.
    /// </remarks>
    public static bool IsRecord(ITypeSymbol type) =>
        type is INamedTypeSymbol { IsRecord: true, TypeArguments.Length: 0 } record &&
        !ScreenplayPrimitiveTypes.TryResolve(record.FullMetadataName(), out _) &&
        record.FindBase(WellKnownTypeNames.ConceptAs) is null;

    /// <summary>
    /// Gets every type reachable through the members of a record.
    /// </summary>
    /// <param name="type">The type to walk.</param>
    /// <returns>The types, ordered so that the same source always reads the same way.</returns>
    /// <remarks>
    /// A record referring to itself, directly or around a loop, is walked once - the second time round would say
    /// nothing new and never end. What comes back is ordered by name rather than by the order the walk happened to
    /// reach it, so that two records naming the same concept differently still leave the same document.
    /// </remarks>
    public static IReadOnlyList<ITypeSymbol> Within(ITypeSymbol type)
    {
        var found = new Dictionary<string, ITypeSymbol>(StringComparer.Ordinal);

        if (IsRecord(type))
        {
            Walk(type, found, new HashSet<string>(StringComparer.Ordinal) { type.ToDisplayString() });
        }

        return [.. found.OrderBy(_ => _.Key, StringComparer.Ordinal).Select(_ => _.Value)];
    }

    /// <summary>
    /// Collects the types the members of one record carry, descending into the records among them.
    /// </summary>
    /// <param name="type">The record to walk.</param>
    /// <param name="found">Everything found so far, keyed by the name it is told apart by.</param>
    /// <param name="walked">The records already walked.</param>
    static void Walk(ITypeSymbol type, Dictionary<string, ITypeSymbol> found, HashSet<string> walked)
    {
        foreach (var property in type.DeclaredProperties())
        {
            var carried = UnderlyingTypes.Of(property.Type);
            var name = carried.ToDisplayString();

            found.TryAdd(name, carried);

            if (IsRecord(carried) && walked.Add(name))
            {
                Walk(carried, found, walked);
            }
        }
    }
}
