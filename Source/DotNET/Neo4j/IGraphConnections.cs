// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Defines a system that resolves the Neo4j connection for the current tenant through the configured connection resolution.
/// </summary>
public interface IGraphConnections
{
    /// <summary>
    /// Resolve the connection for the current tenant.
    /// </summary>
    /// <returns>The resolved <see cref="GraphConnection"/>.</returns>
    GraphConnection Resolve();
}
