// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Cratis.Arc.Validation;
using Cratis.Serialization;
using Cratis.Types;

namespace Cratis.Arc;

/// <summary>
/// Holds the metadata Arc reads off a type universe, built once per <see cref="ITypes"/> instance.
/// </summary>
/// <remarks>
/// <para>
/// Every <c>AddCratisArcCore</c> call used to read the same metadata off the same universe again, which a command or
/// query scenario pays on every initialization. The metadata depends only on the types the universe holds, and a
/// universe never changes its types after construction, so the universe instance is the key: a late generated type
/// discovery provider makes Fundamentals build a new universe, and that new instance gets its own metadata.
/// </para>
/// <para>
/// <see cref="ConditionalWeakTable{TKey, TValue}"/> compares keys by reference, is safe for concurrent readers and
/// writers, hands every caller the one value it stored even when two callers race to build it, and lets a universe
/// nothing else references any more be collected together with its metadata. A factory that throws stores nothing,
/// so a universe whose derived types are invalid keeps throwing on every call, exactly as constructing them did.
/// </para>
/// </remarks>
internal static class TypeUniverseMetadata
{
    static readonly ConditionalWeakTable<ITypes, DerivedTypes> _derivedTypes = new();
    static readonly ConditionalWeakTable<ITypes, FrozenSet<Type>> _discoverableValidators = new();

    /// <summary>
    /// Gets the derived types read off the given universe.
    /// </summary>
    /// <param name="types">The <see cref="ITypes"/> universe to read derived types off.</param>
    /// <returns>The <see cref="DerivedTypes"/> for the universe; the same instance for the same universe.</returns>
    internal static DerivedTypes DerivedTypesFor(ITypes types) =>
        _derivedTypes.GetValue(types, static universe => new DerivedTypes(universe));

    /// <summary>
    /// Gets the discoverable validator types in the given universe.
    /// </summary>
    /// <param name="types">The <see cref="ITypes"/> universe to find validators in.</param>
    /// <returns>The validator types for the universe; the same set for the same universe.</returns>
    internal static FrozenSet<Type> DiscoverableValidatorsIn(ITypes types) =>
        _discoverableValidators.GetValue(types, static universe => universe.FindMultiple(typeof(IDiscoverableValidator<>)).ToFrozenSet());
}
