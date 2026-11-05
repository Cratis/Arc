// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Defines a system that can resolve the name of the Neo4j database for the current tenant.
/// </summary>
public interface IGraphDatabaseNameResolver
{
    /// <summary>
    /// Resolve the name of the Neo4j database.
    /// </summary>
    /// <returns>Name of the database.</returns>
    string Resolve();
}
