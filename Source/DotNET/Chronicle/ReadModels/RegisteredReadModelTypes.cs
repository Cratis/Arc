// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Holds the set of read model types that were registered for command-scoped resolution, so consumers can tell a read
/// model dependency apart from an unrelated one without re-resolving it.
/// </summary>
/// <param name="types">The read model types registered for command-scoped resolution.</param>
public class RegisteredReadModelTypes(IEnumerable<Type> types)
{
    readonly HashSet<Type> _types = [.. types];

    /// <summary>
    /// Determines whether the given type is a registered read model.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a registered read model; otherwise false.</returns>
    public bool Contains(Type type) => _types.Contains(type);
}
