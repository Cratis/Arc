// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc.Validation;

/// <summary>
/// Holds the members <see cref="ModelGraphValidator"/> walks for a model type, as emitted by the Arc source generator
/// for the types reachable from commands and queries.
/// </summary>
/// <remarks>
/// Not intended to be called directly. The generator registers, per exact runtime type, the same properties in the
/// same order as the reflection walk finds them, read through statically typed getters. A type without a registration,
/// such as one the generated code cannot name or a subtype only known at runtime, is walked through reflection. This
/// registry does not imply that the rest of Arc supports trimming or NativeAOT.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ModelGraphWalkers
{
    static readonly ConcurrentDictionary<Type, ModelGraphMember[]> _walkers = new();

    /// <summary>
    /// Registers the members to walk for a type, as emitted by the Arc source generator.
    /// </summary>
    /// <param name="type">The exact runtime type the members are walked for.</param>
    /// <param name="members">The members to walk, in walking order.</param>
    public static void Register(Type type, ModelGraphMember[] members) => _walkers[type] = members;

    /// <summary>
    /// Gets the members registered for an exact type.
    /// </summary>
    /// <param name="type">The runtime type.</param>
    /// <param name="members">The registered members, when the type has a registration.</param>
    /// <returns>True when the type has a registration; otherwise false.</returns>
    internal static bool TryGet(Type type, [NotNullWhen(true)] out ModelGraphMember[]? members) =>
        _walkers.TryGetValue(type, out members);
}
