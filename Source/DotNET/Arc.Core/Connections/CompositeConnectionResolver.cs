// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents an <see cref="IConnectionResolver"/> that resolves through several resolvers in order and merges their results.
/// </summary>
/// <remarks>
/// A part set by a later resolver wins over the same part from an earlier one, see <see cref="ResolvedConnection.Merge"/>.
/// </remarks>
/// <param name="resolvers">The resolvers, in the order they apply.</param>
[IgnoreConvention]
public class CompositeConnectionResolver(IEnumerable<IConnectionResolver> resolvers) : IConnectionResolver
{
    readonly IConnectionResolver[] _resolvers = [.. resolvers];

    /// <inheritdoc/>
    public async Task<ResolvedConnection> Resolve(ConnectionRequest request, CancellationToken cancellationToken = default)
    {
        var result = ResolvedConnection.Unchanged;
        foreach (var resolver in _resolvers)
        {
            result = result.Merge(await resolver.Resolve(request, cancellationToken));
        }

        return result;
    }
}
