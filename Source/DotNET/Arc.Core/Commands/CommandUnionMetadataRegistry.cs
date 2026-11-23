// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Declaration = Cratis.Arc.Commands.CommandUnionMetadata.Declaration;

namespace Cratis.Arc.Commands;

/// <summary>
/// Stores declarations and resolves each previously unseen union through generated registration at most once.
/// </summary>
/// <param name="ensureRegistered">Activates pending generated project modules.</param>
internal sealed class CommandUnionMetadataRegistry(Action ensureRegistered)
{
    readonly ConcurrentDictionary<Type, Declaration> _declarations = new();
    readonly ConcurrentDictionary<Type, Lazy<Declaration?>> _lookups = new();

    /// <summary>
    /// Registers an exact closed union, including one previously resolved through the compatibility path.
    /// </summary>
    /// <param name="unionType">The exact union declaration.</param>
    /// <param name="caseTypes">The case types to snapshot.</param>
    /// <param name="activeValue">The typed active-value accessor.</param>
    internal void Register(Type unionType, Type[] caseTypes, Func<object, object?> activeValue) =>
        _declarations[unionType] = new(Array.AsReadOnly((Type[])caseTypes.Clone()), activeValue);

    /// <summary>
    /// Gets generated metadata, caching absent declarations without hiding subsequent explicit registrations.
    /// </summary>
    /// <param name="unionType">The exact union declaration.</param>
    /// <returns>The declaration, if registered.</returns>
    internal Declaration? For(Type unionType)
    {
        if (_declarations.TryGetValue(unionType, out var declaration)) return declaration;
        return _lookups.GetOrAdd(unionType, static (type, registry) => new(() => registry.Resolve(type)), this).Value;
    }

    Declaration? Resolve(Type unionType)
    {
        ensureRegistered();
        return _declarations.TryGetValue(unionType, out var declaration) ? declaration : null;
    }
}
