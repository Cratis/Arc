// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Concepts;

namespace MongoDB.Driver;

/// <summary>
/// Compares document identifiers in the order MongoDB sorts <c>_id</c> values.
/// </summary>
/// <remarks>
/// Observed queries break ties in the sort field on <c>_id</c> ascending, so a page read from the server and a set
/// maintained in memory agree on the position of documents sharing a sort value. MongoDB compares a standard
/// <see cref="Guid"/> as binary data, byte by byte in its big-endian form, which is not the order
/// <see cref="Guid.CompareTo(Guid)"/> gives, and it compares strings by code unit rather than by culture.
/// </remarks>
internal sealed class DocumentIdComparer : IComparer
{
    /// <summary>
    /// The shared instance.
    /// </summary>
    public static readonly DocumentIdComparer Instance = new();

    /// <inheritdoc/>
    public int Compare(object? x, object? y)
    {
        x = Unwrap(x);
        y = Unwrap(y);
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            (Guid left, Guid right) => CompareGuids(left, right),
            (string left, string right) => string.CompareOrdinal(left, right),
            (IComparable left, _) when left.GetType() == y.GetType() => left.CompareTo(y),
            _ => 0
        };
    }

    static object? Unwrap(object? value) => value?.IsConcept() == true ? value.GetConceptValue() : value;

    static int CompareGuids(Guid left, Guid right) =>
        left.ToByteArray(bigEndian: true).AsSpan().SequenceCompareTo(right.ToByteArray(bigEndian: true));
}
