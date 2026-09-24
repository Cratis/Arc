// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
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
    readonly Lazy<ICanResolveKeyForCommand[]> _resolvers = new(() => Ordered(resolvers).ToArray());

    /// <inheritdoc/>
    public string? GetKeyFor(object command) => GetKeyFor(command, _resolvers.Value);

    /// <summary>
    /// Resolves key rules from an Arc-owned execution scope instead of eagerly capturing root-scoped rules.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="services">The owned execution provider.</param>
    /// <returns>The resolved key, if any.</returns>
    internal string? GetKeyFor(object command, IServiceProvider services) =>
        GetKeyFor(command, Ordered(DiscoveredInstances.ResolvedFrom(services, resolvers)));

    static string? GetKeyFor(object command, IEnumerable<ICanResolveKeyForCommand> selectedResolvers)
    {
        foreach (var resolver in selectedResolvers)
        {
            if (resolver.Resolve(command) is { Length: > 0 } key)
            {
                return key;
            }
        }

        return null;
    }

    static IEnumerable<ICanResolveKeyForCommand> Ordered(IEnumerable<ICanResolveKeyForCommand> selectedResolvers) =>
        selectedResolvers.Where(resolver => resolver is not DefaultKeyForCommandResolver)
            .Concat(selectedResolvers.Where(resolver => resolver is DefaultKeyForCommandResolver));
}
