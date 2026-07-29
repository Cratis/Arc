// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Decides which slice a specification belongs to and what it is called there.
/// </summary>
/// <param name="slices">The namespaces a slice was recovered from.</param>
/// <remarks>
/// A specification sits in a folder beneath the slice it is about, which is a namespace beneath the slice's own. The
/// slice it belongs to is therefore the nearest namespace above it that declares one - nearest rather than any,
/// because a slice within a slice is exactly what a feature holding both a behavior and the behaviors beneath it
/// looks like.
/// <para>
/// The name is every word between the slice and the specification, in the order the source wrote them. That is what
/// the convention was built to read as - a folder saying when something happens and a file saying what else was true
/// at the time - so keeping all of it is what makes the specification say in the document what it says in the source.
/// </para>
/// </remarks>
public class SpecificationPlacement(IEnumerable<string> slices)
{
    /// <summary>The namespace segment holding the world several specifications share.</summary>
    public const string SharedContextSegment = "given";

    readonly HashSet<string> _slices = [.. slices];

    /// <summary>
    /// Gets the name a specification is declared under.
    /// </summary>
    /// <param name="type">The type declaring the specification.</param>
    /// <param name="slice">The namespace of the slice it belongs to.</param>
    /// <returns>The name, as the words the source wrote joined together.</returns>
    public static string NameOf(INamedTypeSymbol type, string slice)
    {
        var below = Segments(type)[SegmentsIn(slice)..];

        return string.Join(
            '_',
            below.Where(_ => !string.Equals(_, SharedContextSegment, StringComparison.Ordinal)).Append(type.Name));
    }

    /// <summary>
    /// Gets the namespace of the slice a specification belongs to.
    /// </summary>
    /// <param name="type">The type declaring the specification.</param>
    /// <returns>The namespace, or <see langword="null"/> when no slice above it declares anything.</returns>
    public string? SliceOf(INamedTypeSymbol type)
    {
        var segments = Segments(type);

        for (var depth = segments.Length - 1; depth > 0; depth--)
        {
            var candidate = string.Join('.', segments[..depth]);
            if (_slices.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the segments of the namespace a type lives in, with the type left out.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <returns>The segments.</returns>
    static string[] Segments(INamedTypeSymbol type) => type.Namespace().Split('.', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Counts the segments of a namespace.
    /// </summary>
    /// <param name="namespace">The namespace to count.</param>
    /// <returns>The number of segments.</returns>
    static int SegmentsIn(string @namespace) => @namespace.Split('.', StringSplitOptions.RemoveEmptyEntries).Length;
}
