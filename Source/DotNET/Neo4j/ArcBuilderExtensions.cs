// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc;

/// <summary>
/// Provides extension methods for <see cref="IArcBuilder"/>.
/// </summary>
public static class ArcBuilderExtensions
{
    /// <summary>
    /// Add Neo4j to the solution: tenant-aware access to the graph databases a Chronicle Neo4j sink writes.
    /// </summary>
    /// <param name="arcBuilder"><see cref="IArcBuilder"/> to use Neo4j with.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="Neo4jOptions"/>.</param>
    /// <param name="configureNeo4j">The optional callback for configuring <see cref="INeo4jBuilder"/>.</param>
    /// <param name="neo4jConfigSectionPath">Optional string for the <see cref="Neo4jOptions"/> config section path.</param>
    /// <returns><see cref="IArcBuilder"/> for building continuation.</returns>
    public static IArcBuilder WithNeo4j(
        this IArcBuilder arcBuilder,
        Action<Neo4jOptions>? configureOptions = default,
        Action<INeo4jBuilder>? configureNeo4j = default,
        string? neo4jConfigSectionPath = null)
    {
        arcBuilder.Services.AddCratisNeo4j(configureOptions, configureNeo4j, neo4jConfigSectionPath);
        return arcBuilder;
    }
}
