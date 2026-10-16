// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents an <see cref="IConnectionResolver"/> that leaves the configured connection as it is.
/// </summary>
public class DefaultConnectionResolver : IConnectionResolver
{
    /// <inheritdoc/>
    public Task<ResolvedConnection> Resolve(ConnectionRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(ResolvedConnection.Unchanged);
}
