// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Arc.Generators;

/// <summary>
/// Represents an immutable array compared by its elements, so incremental generator models holding one compare equal
/// when their contents do.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <param name="items">The elements.</param>
internal sealed class EquatableArray<T>(IEnumerable<T> items) : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    readonly T[] _items = [.. items];

    /// <inheritdoc/>
    public int Count => _items.Length;

    /// <inheritdoc/>
    public T this[int index] => _items[index];

    /// <inheritdoc/>
    public bool Equals(EquatableArray<T>? other) =>
        other is not null && _items.SequenceEqual(other._items);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as EquatableArray<T>);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in _items)
        {
            hash = unchecked((hash * 31) + (item?.GetHashCode() ?? 0));
        }

        return hash;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
