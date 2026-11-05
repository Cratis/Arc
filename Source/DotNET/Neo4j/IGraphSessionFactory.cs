// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Defines a system that opens sessions on the graph database of the current tenant.
/// </summary>
public interface IGraphSessionFactory
{
    /// <summary>
    /// Open a session.
    /// </summary>
    /// <param name="accessMode">The <see cref="AccessMode"/> of the session.</param>
    /// <returns>The <see cref="IGraphSession"/>; dispose it when done.</returns>
    IGraphSession Open(AccessMode accessMode = AccessMode.Read);
}
