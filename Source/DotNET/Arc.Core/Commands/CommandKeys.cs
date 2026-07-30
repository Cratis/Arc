// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Cratis.Types;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandKeys"/> that asks each <see cref="ICanResolveKeyForCommand"/> in
/// turn.
/// </summary>
/// <param name="resolvers">The rules for reading a key from a command.</param>
/// <remarks>
/// The rule Arc ships is asked last regardless of the order the rules are discovered in, so an application's own rule
/// always decides and the outcome never depends on discovery order.
/// </remarks>
[Singleton]
public class CommandKeys(IInstancesOf<ICanResolveKeyForCommand> resolvers) : ICommandKeys
{
    readonly ICanResolveKeyForCommand[] _resolvers =
    [
        .. resolvers.Where(resolver => resolver is not DefaultKeyForCommandResolver),
        .. resolvers.Where(resolver => resolver is DefaultKeyForCommandResolver)
    ];

    /// <inheritdoc/>
    public string? GetKeyFor(object command)
    {
        foreach (var resolver in _resolvers)
        {
            if (resolver.Resolve(command) is { Length: > 0 } key)
            {
                return key;
            }
        }

        return null;
    }
}
