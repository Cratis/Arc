// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.ComponentModel;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Registers complete, exact-type composite key descriptors without runtime member discovery.
/// </summary>
/// <remarks>
/// Register every public readable non-indexed instance property, including inherited ones, before commands run.
/// An explicitly empty descriptor covers a type with no such properties; absence never means an empty descriptor.
/// A base registration does not cover derived runtime types. Manual descriptors are trusted application code.
/// </remarks>
public static class ProtectedReadModelKeyAccessors
{
    static readonly ConcurrentDictionary<Type, Func<object, object?>[]> _readers = new();
    static readonly ConcurrentDictionary<Type, Type[]> _declaredKeys = new();

    /// <summary>
    /// Registers the complete descriptor for an exact composite key type.
    /// </summary>
    /// <typeparam name="T">The exact composite type.</typeparam>
    /// <param name="members">Every public readable non-indexed instance property, read through typed getters.</param>
    /// <exception cref="ProtectedReadModelKeyAccessorsAlreadyRegistered">The type already has a manual or generated descriptor.</exception>
    public static void Register<T>(params ProtectedReadModelKeyMember<T>[] members)
    {
        var readers = members.Select<ProtectedReadModelKeyMember<T>, Func<object, object?>>(member => instance => member.Read((T)instance)).ToArray();
        if (!_readers.TryAdd(typeof(T), readers))
        {
            throw new ProtectedReadModelKeyAccessorsAlreadyRegistered(typeof(T));
        }
    }

    /// <summary>
    /// Records statically declared key types for startup coverage checks; intended for generated metadata only.
    /// </summary>
    /// <param name="commandType">The command declaring the keys.</param>
    /// <param name="keyTypes">The declared key types, without inspecting runtime properties.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void DeclareKeyTypes(Type commandType, params Type[] keyTypes) =>
        _declaredKeys.AddOrUpdate(commandType, static (_, types) => types.ToArray(), static (_, existing, types) => [.. existing.Concat(types).Distinct()], keyTypes);

    /// <summary>
    /// Gets statically known key types for a discovered command.
    /// </summary>
    /// <param name="commandType">The discovered command type.</param>
    /// <returns>The declared key types.</returns>
    internal static IEnumerable<Type> DeclaredFor(Type commandType) =>
        _declaredKeys.TryGetValue(commandType, out var types) ? types : [];

    /// <summary>
    /// Looks up a complete descriptor for the exact runtime type.
    /// </summary>
    /// <param name="type">The exact type.</param>
    /// <param name="readers">The registered property readers, including an explicitly empty descriptor.</param>
    /// <returns>Whether a descriptor exists.</returns>
    internal static bool TryGet(Type type, out Func<object, object?>[] readers) => _readers.TryGetValue(type, out readers!);
}
