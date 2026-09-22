// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a collection emitted by an observable query that already knows which of its items changed.
/// </summary>
/// <remarks>
/// An observable query provider that watches a data source - a MongoDB change stream, for example - is told exactly
/// which document changed. Without a way to pass that on, the emission is just a new snapshot and the delta has to be
/// rediscovered downstream by comparing every item of the new snapshot against every item of the previous one, which
/// costs time proportional to the size of the collection for a change that touched a single row.
/// <para>
/// An emitted collection implementing this states its own delta instead. The interface is deliberately additive: the
/// emitted value is still an <see cref="IEnumerable{T}"/> of the read model, so a subscriber that does not know about
/// this sees no difference, and a provider that cannot know what changed simply does not implement it and is served by
/// the comparison fallback.
/// </para>
/// </remarks>
public interface IHaveKnownChanges
{
    /// <summary>
    /// Gets the changes this emission represents, or <see langword="null"/> when the emission is a whole snapshot
    /// rather than a delta - the initial query result, or a re-read after reconnecting.
    /// </summary>
    IReadOnlyList<CollectionChange>? Changes { get; }
}
