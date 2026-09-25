// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Arc.Queries;

/// <summary>
/// A snapshot emitted by an observable collection query that carries the changes it represents.
/// </summary>
/// <typeparam name="TDocument">Type of item in the collection.</typeparam>
/// <param name="items">The items making up the snapshot.</param>
/// <param name="changes">The changes this snapshot represents, or <see langword="null"/> when it is a whole snapshot.</param>
/// <remarks>
/// This is an ordinary <see cref="IEnumerable{T}"/> to every subscriber that does not care where the delta came from,
/// so emitting it in place of a plain array changes nothing for existing consumers.
/// </remarks>
public sealed class ObservedCollection<TDocument>(
    IReadOnlyList<TDocument> items,
    IReadOnlyList<CollectionChange>? changes) : IReadOnlyList<TDocument>, IHaveKnownChanges
{
    /// <inheritdoc/>
    public IReadOnlyList<CollectionChange>? Changes { get; } = changes;

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public TDocument this[int index] => items[index];

    /// <inheritdoc/>
    public IEnumerator<TDocument> GetEnumerator() => items.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
