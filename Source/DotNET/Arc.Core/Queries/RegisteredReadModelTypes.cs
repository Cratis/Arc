// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Holds the set of read model types that were registered for command-scoped resolution, so consumers can tell a read
/// model dependency apart from an unrelated one without re-resolving it.
/// </summary>
/// <remarks>
/// The set is additive across every provider that contributes a command-scoped resolver: a read model backed by
/// Chronicle and one backed by another provider (for example Entity Framework Core) are both registered here, so the
/// classification of a missing read model as invalid client input works uniformly regardless of where the read model is
/// materialized.
/// </remarks>
/// <param name="types">The read model types registered for command-scoped resolution.</param>
public class RegisteredReadModelTypes(IEnumerable<Type> types)
{
    readonly HashSet<Type> _types = [.. types];

    /// <summary>
    /// Gets the read model types registered for command-scoped resolution.
    /// </summary>
    public IEnumerable<Type> Types => _types;

    /// <summary>
    /// Determines whether the given type is a registered read model.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a registered read model; otherwise false.</returns>
    public bool Contains(Type type) => _types.Contains(type);
}
