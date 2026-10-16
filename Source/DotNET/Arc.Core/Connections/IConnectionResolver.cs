// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Defines a system that can resolve how to connect to a storage.
/// </summary>
public interface IConnectionResolver
{
    /// <summary>
    /// Resolve a connection.
    /// </summary>
    /// <param name="request">The <see cref="ConnectionRequest"/> to resolve for.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="ResolvedConnection"/>. Use <see cref="ResolvedConnection.Unchanged"/> to keep the configured connection.</returns>
    Task<ResolvedConnection> Resolve(ConnectionRequest request, CancellationToken cancellationToken = default);
}
